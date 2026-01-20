# Technical Design: outlook-file-handling

## Status
- **Phase**: Design (Generated)
- **Created**: 2026-01-20
- **Updated**: 2026-01-20

---

## Overview

Outlookメールファイル（.msg, .eml）を開いた際に`ExternalWindowReady`イベントが発火されず、LoadingWindowが閉じないバグを修正する。

**修正方針**: `OpenWithDefaultProgram`メソッド内で`OpenEmailFile`呼び出し後に`ExternalWindowReady`イベントを発火する

---

## Architecture Pattern & Boundary Map

### 現在のイベントフロー（問題あり）

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   MainWindow    │     │  ViewerWindow   │     │  LoadingWindow  │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │ OpenDocument()        │                       │
         ├──────────────────────>│                       │
         │                       │                       │
         │ new LoadingWindow()   │                       │
         ├───────────────────────┼──────────────────────>│
         │                       │                       │ Show()
         │                       │                       │
         │ ExternalWindowReady   │                       │
         │ (購読)                │                       │
         │<──────────────────────│                       │
         │                       │                       │
         │                       │ OpenEmailFile()       │
         │                       ├──────┐                │
         │                       │      │ WaitForOutlook │
         │                       │<─────┘                │
         │                       │                       │
         │                       │ ★イベント発火なし★   │
         │                       │                       │
         │                       │                       │ ← 閉じない
```

### 修正後のイベントフロー

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   MainWindow    │     │  ViewerWindow   │     │  LoadingWindow  │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │ OpenDocument()        │                       │
         ├──────────────────────>│                       │
         │                       │                       │
         │ new LoadingWindow()   │                       │
         ├───────────────────────┼──────────────────────>│
         │                       │                       │ Show()
         │                       │                       │
         │ ExternalWindowReady   │                       │
         │ (購読)                │                       │
         │<──────────────────────│                       │
         │                       │                       │
         │                       │ OpenEmailFile()       │
         │                       ├──────┐                │
         │                       │      │ WaitForOutlook │
         │                       │<─────┘                │
         │                       │                       │
         │ ExternalWindowReady   │ ★イベント発火★       │
         │<──────────────────────│                       │
         │                       │                       │
         │ loadingWindow.Close() │                       │
         ├───────────────────────┼──────────────────────>│
         │                       │                       │ Close()
```

---

## Technology Stack & Alignment

| 技術 | 現行 | 修正後 | 備考 |
|------|------|--------|------|
| WPF | .NET 9.0-windows | 変更なし | - |
| イベントパターン | EventHandler<IntPtr> | 変更なし | 既存パターン踏襲 |
| スレッド制御 | Dispatcher.Invoke | 変更なし | UIスレッドへのマーシャリング |
| ウィンドウ検出 | Process.GetProcesses | 変更なし | Win32 API連携 |

---

## Components & Interface Contracts

### 修正対象コンポーネント

#### ViewerWindow.xaml.cs

**修正箇所**: `OpenWithDefaultProgram`メソッド（334-338行目付近）

**現在のコード**:
```csharp
// メールファイルの場合は専用処理
if (extension is ".msg" or ".eml")
{
    _externalWindowHandle = OpenEmailFile(filePath);
    return _externalWindowHandle;  // ← イベント発火なしで終了
}
```

**修正後のコード**:
```csharp
// メールファイルの場合は専用処理
if (extension is ".msg" or ".eml")
{
    _externalWindowHandle = OpenEmailFile(filePath);

    // ExternalWindowReadyイベントを発火（LoadingWindow閉じるため）
    Dispatcher.Invoke(() => ExternalWindowReady?.Invoke(this, _externalWindowHandle));

    return _externalWindowHandle;
}
```

---

## Requirements Traceability

| 要件ID | 要件概要 | 対応コンポーネント | 対応方法 |
|--------|---------|-------------------|----------|
| 1.1 | ExternalWindowReadyイベントの発火 | ViewerWindow.xaml.cs | イベント発火追加 |
| 1.2 | タイムアウト時のフォールバック | OpenEmailFile内 | 既存実装で対応済み（IntPtr.Zero返却） |
| 2.1 | ChecklistWindow表示 | MainWindow.xaml.cs | 1.1の修正により自動解決 |
| 3.1 | Outlook未インストール時対応 | OpenEmailFile内 | 既存try-catchで対応済み |
| 3.2 | ファイル破損時対応 | OpenEmailFile内 | 既存try-catchで対応済み |

---

## Interface Definitions

### ExternalWindowReadyイベント（既存）

```csharp
/// <summary>
/// 外部プログラムのウィンドウが準備完了したときに発生するイベント
/// </summary>
public event EventHandler<IntPtr>? ExternalWindowReady;
```

**パラメータ**:
- `IntPtr`: 外部ウィンドウのハンドル（検出失敗時は`IntPtr.Zero`）

**購読側（MainWindow）の処理**:
```csharp
viewerWindow.ExternalWindowReady += (sender, windowHandle) =>
{
    // 読み込み画面を閉じる
    loadingWindow?.Close();

    // 外部ウィンドウ配置
    if (windowHandle != IntPtr.Zero)
    {
        PositionExternalWindowOnPrimaryMonitor(windowHandle);
    }

    // ChecklistWindow表示
    OpenChecklistWindow(document, windowHandle);
};
```

---

## Error Handling Strategy

### エラーシナリオと対応

| シナリオ | 検出方法 | 対応 |
|---------|---------|------|
| Outlook未インストール | Process.Start例外 | MessageBox表示、IntPtr.Zero返却 |
| ファイル破損 | Process.Start例外 | MessageBox表示、IntPtr.Zero返却 |
| ウィンドウ検出タイムアウト | 30秒ループ完了 | 既存ウィンドウ使用 or IntPtr.Zero返却 |

**共通**: すべてのケースで`ExternalWindowReady`イベントが発火され、LoadingWindowは閉じる

---

## Testing Strategy

### 単体テスト

| テストケース | 期待結果 |
|-------------|---------|
| .msgファイルを開く | LoadingWindowが表示後、自動的に閉じる |
| Outlook起動成功 | ExternalWindowReadyイベントにウィンドウハンドルが渡される |
| Outlook起動タイムアウト | ExternalWindowReadyイベントにIntPtr.Zeroが渡される |
| Outlook未インストール | エラーメッセージ表示後、LoadingWindowが閉じる |

### 手動テスト

1. .msgファイルをダブルクリック
2. LoadingWindowが表示されることを確認
3. Outlookが起動することを確認
4. LoadingWindowが閉じることを確認
5. ChecklistWindowが表示されることを確認

---

## Implementation Notes

### 変更ファイル

| ファイル | 変更内容 | 行数目安 |
|---------|---------|---------|
| `src/DocumentFileManager.Viewer/ViewerWindow.xaml.cs` | イベント発火追加 | +3行 |

### 影響範囲

- **影響あり**: メールファイル（.msg, .eml）を開く処理
- **影響なし**: 他のファイル形式（Office、CAD、画像等）

### 後方互換性

完全に後方互換。既存の動作に変更なし。
