# キャプチャ座標のDPI対応修正プラン

## 問題の背景

### 現状の問題
マルチモニター環境で異なるDPIのモニター間を移動した際、キャプチャの範囲選択座標がずれる。

### 根本原因
1. **WPFのDPI処理の複雑さ**: WPFは論理座標（DPI非依存）を使用するが、`Screen.Bounds`は物理座標を返す
2. **Per-Monitor DPI非対応**: アプリケーションにDPI Awarenessマニフェストがないため、WPFのDPIスケールはプライマリモニターのDPIを基準にしている
3. **座標系の混在**:
   - `GetVirtualScreenBounds()` → 物理座標
   - WPF Window の `Left/Top/Width/Height` → 論理座標（プライマリモニターDPI基準）
   - `e.GetPosition(this)` → WPFの論理座標
   - `ScreenCaptureService.CaptureRectangle()` → 物理座標が必要

## 解決アプローチ

### アプローチ1: 選択領域の中心座標からモニターを特定してDPIを取得（推奨）

**概要**: マウスアップ時に選択範囲の中心座標がどのモニターにあるかを特定し、そのモニターのDPIを使用して座標変換する。

**メリット**:
- Per-Monitor DPI Awarenessなしでも動作可能
- 既存のコード構造を大きく変えずに対応可能
- 選択範囲が主にあるモニターのDPIを使用するため、精度が高い

**デメリット**:
- 複数モニターにまたがる選択の場合、完全な精度は保証されない

### アプローチ2: Win32 API `GetDpiForMonitor` を使用

**概要**: Win32 APIを直接呼び出して特定モニターのDPIを取得する。

**メリット**:
- 正確なPer-Monitor DPIを取得可能

**デメリット**:
- P/Invoke追加が必要
- Windows 8.1以降が必要

### アプローチ3: アプリケーションをPer-Monitor DPI Awareに設定

**概要**: app.manifestでPer-Monitor DPI Awarenessを有効化する。

**メリット**:
- WPFが自動的にPer-Monitor DPIを処理
- 最も正確なDPI処理

**デメリット**:
- アプリケーション全体に影響
- 他のUI要素にも影響を与える可能性
- テスト範囲が広がる

## 推奨アプローチ: アプローチ1 + 2の組み合わせ

選択範囲の中心座標からモニターを特定し、`GetDpiForMonitor` APIでそのモニターのDPIを取得する。

## 実装計画

### Step 1: モニターDPI取得メソッドの追加

```csharp
// Win32 API宣言
[DllImport("shcore.dll")]
private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

[DllImport("user32.dll")]
private static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint dwFlags);

private const int MDT_EFFECTIVE_DPI = 0;
private const uint MONITOR_DEFAULTTONEAREST = 2;

/// <summary>
/// 指定座標のモニターのDPIを取得
/// </summary>
private (double scaleX, double scaleY) GetDpiForPoint(double x, double y)
{
    var point = new System.Drawing.Point((int)x, (int)y);
    var monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);

    if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY) == 0)
    {
        return (dpiX / 96.0, dpiY / 96.0);
    }

    // フォールバック: プライマリモニターのDPI
    return (_dpiScaleX, _dpiScaleY);
}
```

### Step 2: Window_MouseUp の座標変換修正

```csharp
private void Window_MouseUp(object sender, MouseButtonEventArgs e)
{
    if (_isSelecting)
    {
        _isSelecting = false;
        var currentPoint = e.GetPosition(this);

        // 選択範囲が小さすぎる場合はキャンセル
        var logicalWidth = Math.Abs(currentPoint.X - _startPoint.X);
        var logicalHeight = Math.Abs(currentPoint.Y - _startPoint.Y);

        if (logicalWidth < 10 || logicalHeight < 10)
        {
            DialogResult = false;
            Close();
            return;
        }

        var virtualScreen = GetVirtualScreenBounds();
        var logicalX = Math.Min(_startPoint.X, currentPoint.X);
        var logicalY = Math.Min(_startPoint.Y, currentPoint.Y);

        // プライマリモニターのDPIで仮の物理座標を計算
        var primaryDpiScale = GetPrimaryMonitorDpiScale();
        var approxPhysicalX = virtualScreen.Left + (logicalX * primaryDpiScale.scaleX);
        var approxPhysicalY = virtualScreen.Top + (logicalY * primaryDpiScale.scaleY);

        // 選択範囲の中心座標のモニターDPIを取得
        var centerX = approxPhysicalX + (logicalWidth * primaryDpiScale.scaleX / 2);
        var centerY = approxPhysicalY + (logicalHeight * primaryDpiScale.scaleY / 2);
        var (targetDpiScaleX, targetDpiScaleY) = GetDpiForPoint(centerX, centerY);

        // ターゲットモニターのDPIで正確な物理座標を計算
        var physicalX = virtualScreen.Left + (logicalX * targetDpiScaleX);
        var physicalY = virtualScreen.Top + (logicalY * targetDpiScaleY);
        var physicalWidth = logicalWidth * targetDpiScaleX;
        var physicalHeight = logicalHeight * targetDpiScaleY;

        SelectedArea = new Rect(physicalX, physicalY, physicalWidth, physicalHeight);
        DialogResult = true;
        Close();
    }
}
```

### Step 3: プライマリモニターDPI取得メソッド

```csharp
/// <summary>
/// プライマリモニターのDPIスケールを取得
/// </summary>
private (double scaleX, double scaleY) GetPrimaryMonitorDpiScale()
{
    var source = PresentationSource.FromVisual(this);
    if (source?.CompositionTarget != null)
    {
        return (
            source.CompositionTarget.TransformToDevice.M11,
            source.CompositionTarget.TransformToDevice.M22
        );
    }
    return (1.0, 1.0);
}
```

### Step 4: 初期選択範囲のDPI対応（オプション）

初期選択範囲表示時も、正確なモニターDPIを使用して座標変換する。

## ファイル変更一覧

| ファイル | 変更内容 |
|---------|---------|
| `ScreenCaptureOverlay.xaml.cs` | Win32 API追加、DPI取得メソッド追加、座標変換修正 |

## テスト項目

1. **同一DPIモニター間**: 100%→100% でキャプチャが正確か
2. **高DPI→低DPIモニター**: 150%→100% でキャプチャが正確か
3. **低DPI→高DPIモニター**: 100%→150% でキャプチャが正確か
4. **初期選択範囲**: ウィンドウ範囲が正しく表示されるか
5. **モニターをまたぐ選択**: 2つのモニターにまたがる選択が動作するか

## リスクと対策

| リスク | 対策 |
|-------|------|
| GetDpiForMonitor が古いWindowsで失敗 | フォールバックとしてWPFのDPIスケールを使用 |
| 複数モニターにまたがる選択 | 中心座標のモニターDPIを使用（許容できる精度） |
