# DocumentFileManager.Viewer 実装プラン

**作成日**: 2025-10-08
**目的**: 資料ファイルを画面左2/3に表示する専用ビューアーアプリケーションの実装

---

## 概要

DocumentFileManager.Viewerは、資料ファイルを画面左側2/3に表示し、ChecklistWindow（右1/3）と連携することで、画面全体を効率的に活用するビューアーアプリケーションです。

### 主な特徴
- ✅ 確実なウィンドウ制御（自前アプリなのでProcess.MainWindowHandleで100%取得可能）
- ✅ 高速起動（軽量ビューアー）
- ✅ 完全な位置制御（左2/3固定、移動ブロック）
- ✅ 統一されたUX（全ファイル形式で一貫したUI/操作感）
- ✅ 拡張性（将来的に注釈・マーカー・検索等の機能追加可能）
- ✅ 柔軟性（サポート対象外はWindows標準プログラムで開く）

---

## プロジェクト構成

```
src/DocumentFileManager.Viewer/
├── App.xaml                      # アプリケーションエントリポイント
├── App.xaml.cs
├── ViewerWindow.xaml             # メインウィンドウUI
├── ViewerWindow.xaml.cs          # メインウィンドウロジック
├── Viewers/
│   ├── PdfViewer.xaml            # PDFビューアー
│   ├── PdfViewer.xaml.cs
│   ├── ImageViewer.xaml          # 画像ビューアー
│   ├── ImageViewer.xaml.cs
│   ├── TextViewer.xaml           # テキストビューアー
│   ├── TextViewer.xaml.cs
│   ├── EmailViewer.xaml          # Outlookメールビューアー
│   └── EmailViewer.xaml.cs
├── Models/
│   └── EmailMessage.cs           # メール情報モデル
└── Properties/
    └── launchSettings.json
```

---

## 対応ファイル形式

### 専用Viewerで表示（左2/3配置 + ChecklistWindow連携）

| 形式 | 拡張子 | ビューアー | テストファイル | 優先度 |
|------|--------|-----------|--------------|--------|
| PDF | `.pdf` | PdfViewer | sample.pdf | 高 |
| 画像 | `.png`, `.jpg`, `.gif` | ImageViewer | sample.png/jpg/gif | 高 |
| テキスト | `.txt`, `.log`, `.csv`, `.md` | TextViewer | sample.txt/log/csv/md | 高 |
| Outlookメール | `.msg` | EmailViewer | sample.msg | 中 |

### Windows標準プログラムで開く（ChecklistWindow非連携）

| 形式 | 拡張子 | テストファイル | 備考 |
|------|--------|--------------|------|
| Office文書 | `.docx`, `.doc`, `.xlsx`, `.xls`, `.pptx`, `.ppt` | sample.docx等 | Word/Excel/PowerPoint |
| CADファイル | `.3dm`, `.sldprt`, `.sldasm`, `.dwg` | sample.3dm等 | Rhino/Solidworks/AutoCAD |

---

## NuGetパッケージ

### 必須パッケージ
- **PDFiumSharp** - PDF表示（Apache 2.0ライセンス）
- **MsgReader** - .msgファイル読み込み（MITライセンス）

### オプションパッケージ（将来追加予定）
- **MimeKit** - .emlファイル読み込み（MITライセンス）

---

## 実装フェーズ

### フェーズ1: プロジェクト基盤（1日目）

#### Step 1.1: プロジェクト作成
```bash
cd src
dotnet new wpf -n DocumentFileManager.Viewer -f net9.0-windows
cd DocumentFileManager.Viewer
dotnet add reference ../DocumentFileManager/DocumentFileManager.csproj
cd ../..
dotnet sln add src/DocumentFileManager.Viewer/DocumentFileManager.Viewer.csproj
```

#### Step 1.2: ViewerWindow基本構造
- コマンドライン引数処理（ファイルパス受け取り）
- ウィンドウ位置制御（画面左2/3固定）
- Win32 APIで移動ブロック（ChecklistWindowと同様の実装）

#### Step 1.3: ファイル振り分けロジック
```csharp
private bool IsSupportedFile(string extension)
{
    return extension is ".pdf" or ".png" or ".jpg" or ".gif"
        or ".txt" or ".log" or ".csv" or ".md" or ".msg";
}

private void LoadFile(string filePath)
{
    if (IsSupportedFile(extension))
    {
        LoadInViewer(filePath, extension); // 専用Viewerで表示
    }
    else
    {
        OpenWithDefaultProgram(filePath);  // Windows標準プログラム
        Close();
    }
}
```

---

### フェーズ2: 基本Viewer実装（2日目）

#### Step 2.1: ImageViewer実装（最優先・最もシンプル）

**UI**: WPF標準 Image + ScrollViewer
```xaml
<ScrollViewer>
    <Image x:Name="ImageControl" Stretch="Uniform"/>
</ScrollViewer>
```

**機能**:
- 画像読み込み（PNG/JPG/GIF）
- ズーム機能（Ctrl+マウスホイール）
- パン機能（ドラッグ移動）

**テスト**:
- `test-files/images/sample.png` (21.13 KB)
- `test-files/images/sample.jpg` (10.65 KB)
- `test-files/images/sample.gif` (3.75 KB)

#### Step 2.2: TextViewer実装

**UI**: WPF標準 TextBox（ReadOnly）
```xaml
<TextBox x:Name="TextControl"
         IsReadOnly="True"
         VerticalScrollBarVisibility="Auto"
         FontFamily="Consolas"
         FontSize="12"/>
```

**機能**:
- テキスト読み込み（UTF-8、Shift-JIS自動判定）
- スクロール表示

**テスト**:
- `test-files/text/sample.txt` (23.15 KB)
- `test-files/text/sample.log` (12.98 KB)
- `test-files/text/sample.csv` (0.1 KB)
- `test-files/text/sample.md` (0.2 KB)

---

### フェーズ3: PDF対応（3日目）

#### Step 3.1: PdfViewer実装

**NuGetパッケージ追加**:
```bash
dotnet add package PDFiumSharp
```

**ライブラリ使用例**:
```csharp
using PDFiumSharp;

public void LoadPdf(string filePath)
{
    using var document = new PdfDocument(filePath);
    for (int i = 0; i < document.Pages.Count; i++)
    {
        using var page = document.Pages[i];
        var bitmap = page.Render(1920, 1080);
        // Imageコントロールに表示
    }
}
```

**機能**:
- ページ表示（ScrollViewer + StackPanel）
- ズーム機能
- ページナビゲーション（前/次ボタン）

**テスト**:
- `test-files/pdf/sample.pdf` (40.39 KB)

---

### フェーズ4: Email対応（4日目）

#### Step 4.1: EmailViewer実装

**NuGetパッケージ追加**:
```bash
dotnet add package MsgReader
```

**Models作成**: `Models/EmailMessage.cs`
```csharp
public class EmailMessage
{
    public string Subject { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Cc { get; set; }
    public DateTime SentDate { get; set; }
    public string BodyHtml { get; set; }
    public string BodyText { get; set; }
    public List<EmailAttachment> Attachments { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; }
    public string FileSize { get; set; }
    public byte[] Data { get; set; }
}
```

**ライブラリ使用例**:
```csharp
using MsgReader.Outlook;

public EmailMessage LoadMsg(string filePath)
{
    using var msg = new Storage.Message(filePath);

    return new EmailMessage
    {
        Subject = msg.Subject,
        From = msg.Sender?.Email ?? "不明",
        To = string.Join("; ", msg.Recipients
            .Where(r => r.Type == RecipientType.To)
            .Select(r => r.Email)),
        SentDate = msg.SentOn ?? DateTime.MinValue,
        BodyHtml = msg.BodyHtml ?? msg.BodyText,
        Attachments = msg.Attachments.Select(a => new EmailAttachment
        {
            FileName = a.FileName,
            FileSize = FormatFileSize(a.Data?.Length ?? 0),
            Data = a.Data
        }).ToList()
    };
}
```

**UI構成**:
- ヘッダー情報（件名、差出人、宛先、日時）
- 本文（WebBrowser コントロールでHTML表示）
- 添付ファイル一覧（ListView）

**テスト**:
- `test-files/email/sample.msg` (128 KB)

---

### フェーズ5: MainWindow連携（5日目）

#### Step 5.1: MainWindow.xaml.cs修正

**変更箇所**: `DocumentsListView_MouseDoubleClick` メソッド

```csharp
private void DocumentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (DocumentsListView.SelectedItem is Document document)
    {
        var absolutePath = Path.Combine(GetProjectRoot(), document.RelativePath);

        if (!File.Exists(absolutePath))
        {
            MessageBox.Show("ファイルが見つかりません");
            return;
        }

        // Viewerプロジェクトのパス
        var viewerPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "DocumentFileManager.Viewer.exe");

        if (File.Exists(viewerPath))
        {
            // Viewer経由で開く
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = viewerPath,
                Arguments = $"\"{absolutePath}\"",
                UseShellExecute = false
            });

            // サポート対象ファイルの場合のみChecklistWindowを開く
            var extension = Path.GetExtension(absolutePath).ToLower();
            if (IsSupportedByViewer(extension))
            {
                OpenChecklistWindow(document);
            }
        }
        else
        {
            // Viewerがない場合は従来通りデフォルトアプリで開く
            Process.Start(new ProcessStartInfo
            {
                FileName = absolutePath,
                UseShellExecute = true
            });
        }
    }
}

private bool IsSupportedByViewer(string extension)
{
    var supportedExtensions = new[]
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".gif",
        ".txt", ".log", ".csv", ".md", ".msg"
    };
    return supportedExtensions.Contains(extension);
}
```

#### Step 5.2: 統合テスト

**サポート対象ファイル**（Viewer起動 + ChecklistWindow連携）:
- PDF
- 画像（PNG/JPG/GIF）
- テキスト（TXT/LOG/CSV/MD）
- Email（MSG）

**サポート対象外ファイル**（Windows標準プログラム + ChecklistWindow非連携）:
- Office（DOCX/DOC/XLSX/XLS/PPTX/PPT）
- CAD（3DM/SLDPRT/SLDASM/DWG）

---

### フェーズ6: 最終調整（6日目）

#### Step 6.1: エラーハンドリング強化
- ファイル読み込みエラー
- ライブラリエラー（PDFiumSharp/MsgReader）
- ウィンドウ制御エラー

#### Step 6.2: ログ出力追加
```csharp
_logger.LogInformation("Viewer起動: {FilePath}", filePath);
_logger.LogError(ex, "ファイル読み込み失敗: {FilePath}", filePath);
```

#### Step 6.3: ドキュメント更新
- `docs/FEATURES.md` - Viewer機能追加
- `docs/ARCHITECTURE.md` - Viewerプロジェクト追加
- `README.md` - 使い方更新

---

## 画面レイアウト（完成イメージ）

```
┌────────────────────────────────────┬────────────┐
│ DocumentFileManager.Viewer         │ Checklist  │
│ (左2/3、固定位置)                  │ Window     │
│ ┌──────────────────────────────┐  │ (右1/3)    │
│ │                               │  │            │
│ │  PDF / 画像 / テキスト /      │  │ □ 項目1   │
│ │  Outlookメール 表示エリア     │  │ □ 項目2   │
│ │                               │  │ ✓ 項目3   │
│ │  [ズーム] [前へ] [次へ]       │  │ □ 項目4   │
│ │                               │  │            │
│ └──────────────────────────────┘  │            │
└────────────────────────────────────┴────────────┘
```

---

## 技術的詳細

### ウィンドウ位置制御

```csharp
private void InitializeWindowPosition()
{
    var workArea = SystemParameters.WorkArea;
    Width = workArea.Width * 2.0 / 3.0;  // 左2/3
    Height = workArea.Height;            // 全画面高さ
    Left = workArea.Left;                // 画面左端
    Top = workArea.Top;                  // 画面上端

    // Win32 APIで移動ブロック（ChecklistWindowと同様）
    WindowStyle = WindowStyle.ToolWindow;
    ResizeMode = ResizeMode.CanResize;
}

// Win32 API（移動ブロック）
private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    if (msg == WM_WINDOWPOSCHANGING && !_isAdjustingPosition)
    {
        var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
        windowPos.flags |= SWP_NOMOVE;
        Marshal.StructureToPtr(windowPos, lParam, true);
        handled = false;
    }
    return IntPtr.Zero;
}
```

---

## 制約事項

### 表示専用
⚠️ 編集機能は提供しません（将来的に「外部アプリで開く」ボタン追加可能）

### ライブラリ依存
- PDFiumSharp (Apache 2.0) - ✅ 商用利用可
- MsgReader (MIT) - ✅ 商用利用可

### 初期バージョン非対応
- ❌ .emlファイル（必要に応じてMimeKit追加）
- ❌ .bmp, .jpeg（.png/.jpgで代替可能）

---

## テストファイル

すべてのテストファイルは `test-files/` ディレクトリに配置済み（19/22ファイル収集完了）:

```
test-files/
├── pdf/sample.pdf (40.39 KB)
├── images/
│   ├── sample.png (21.13 KB)
│   ├── sample.jpg (10.65 KB)
│   └── sample.gif (3.75 KB)
├── text/
│   ├── sample.txt (23.15 KB)
│   ├── sample.log (12.98 KB)
│   ├── sample.csv (0.1 KB)
│   └── sample.md (0.2 KB)
├── email/sample.msg (128 KB)
├── office/ (DOCX/DOC/XLSX/XLS/PPTX/PPT)
└── cad/ (3DM/SLDPRT/SLDASM/DWG)
```

---

## 実装チェックリスト

- [ ] Step 0: 実装プラン保存 ✅
- [ ] Step 1: プロジェクト作成
- [ ] Step 2: ViewerWindow基本実装
- [ ] Step 3: ImageViewer実装・テスト
- [ ] Step 4: TextViewer実装・テスト
- [ ] Step 5: PdfViewer実装・テスト
- [ ] Step 6: EmailViewer実装・テスト
- [ ] Step 7: MainWindow連携・統合テスト
- [ ] Step 8: エラーハンドリング・最終調整・ドキュメント更新

---

**次のステップ**: Step 1（プロジェクト作成）に進む
