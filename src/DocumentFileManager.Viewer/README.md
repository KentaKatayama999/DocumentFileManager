# DocumentFileManager.Viewer

統合ドキュメントビューアは、様々なファイル形式をシームレスに表示するスタンドアロンWPFアプリケーションです。

## 概要

DocumentFileManager.Viewerは、技術資料管理アプリケーションの一部として開発された汎用ドキュメントビューアです。画像、テキスト、PDF、Office、CAD、メールファイルなど、様々な形式のファイルを統一的なインターフェースで表示します。

## 対応ファイル形式

### 内部ビューア表示

以下のファイル形式は、Viewerウィンドウ内に直接表示されます：

- **画像ファイル**: `.png`, `.jpg`, `.jpeg`, `.gif`
  - WPFの`Image`コントロールで表示
  - 自動サイズ調整、ズーム機能対応

- **テキストファイル**: `.txt`, `.log`, `.csv`, `.md`
  - 構文ハイライト対応
  - 大容量ファイル対応

- **PDFファイル**: `.pdf`
  - WebView2による高品質レンダリング
  - ページナビゲーション、ズーム機能完全対応

### 外部プログラム連携（自動ウィンドウ配置）

以下のファイル形式は、Windows標準プログラムで開き、ウィンドウを自動配置します：

- **Officeファイル**: `.doc`, `.docx`, `.xls`, `.xlsx`, `.xlsm`, `.xlm`, `.ppt`, `.pptx`
  - Microsoft Office（Word、Excel、PowerPoint）で開く
  - 既存インスタンスの再利用をサポート

- **CADファイル**: `.3dm`, `.sldprt`, `.sldasm`, `.dwg`, `.igs`, `.iges`
  - Rhinoceros、SolidWorks、AutoCADなど各PCの既定プログラムで開く

- **メールファイル**: `.msg`, `.eml`
  - Microsoft Outlookで開く
  - 新規ウィンドウの自動検出

## 主な機能

### ウィンドウ管理

- **固定ビューアウィンドウ**
  - 画面左端に固定（ドラッグで移動不可）
  - 画面の左2/3幅×全高で表示
  - 高さ固定（最大化・最小化のみ可能）

- **外部プログラム自動配置**
  - 開いたファイルのウィンドウを画面左2/3に自動リサイズ・配置
  - Win32 API（SetWindowPos）による正確な制御
  - 最大化状態の自動解除

### ウィンドウハンドル管理

- **ウィンドウハンドル取得API**
  ```csharp
  var viewerWindow = new ViewerWindow(filePath);
  IntPtr handle = viewerWindow.GetWindowHandle();
  ```
  - 内部ビューア: ViewerWindow自体のハンドルを返す
  - 外部プログラム: 開いたファイルのウィンドウハンドルを返す

### プロセス検出

- **既存インスタンス対応**
  - Excel、Word、PowerPointなどの既存インスタンスを正しく検出
  - プロセス名とウィンドウタイトルによる高精度マッチング
  - スプラッシュスクリーンの誤検出を回避

- **ポーリング方式**
  - 最大120秒間、500ms間隔でウィンドウを検索
  - タイムアウト時の詳細エラーメッセージ

## 使用方法

### 基本的な使用方法

```bash
# ビルド
cd src/DocumentFileManager.Viewer
dotnet build

# 実行
./bin/Debug/net9.0-windows/DocumentFileManager.Viewer.exe <ファイルパス>

# 例: PDFファイルを開く
./bin/Debug/net9.0-windows/DocumentFileManager.Viewer.exe "D:\documents\report.pdf"

# 例: Excelファイルを開く
./bin/Debug/net9.0-windows/DocumentFileManager.Viewer.exe "D:\data\sales.xlsx"
```

### プログラムからの使用

```csharp
using DocumentFileManager.Viewer;

// ViewerWindowを作成
var viewerWindow = new ViewerWindow(filePath);
viewerWindow.Show();

// ウィンドウハンドルを取得
IntPtr handle = viewerWindow.GetWindowHandle();

// ハンドルを使用して追加の操作が可能
```

## アーキテクチャ

### クラス構成

```
DocumentFileManager.Viewer/
├── App.xaml.cs                    # アプリケーションエントリポイント
├── ViewerWindow.xaml.cs           # メインウィンドウ
│   ├── OpenWithDefaultProgram()   # 外部プログラムで開く
│   ├── OpenEmailFile()            # メールファイル専用処理
│   ├── GetOutlookWindowHandles()  # Outlookウィンドウ検出
│   ├── PositionExternalWindow()   # ウィンドウ自動配置
│   └── GetWindowHandle()          # ウィンドウハンドル取得API
└── Viewers/
    ├── ImageViewer.xaml.cs        # 画像ビューア
    ├── TextViewer.xaml.cs         # テキストビューア
    └── PdfViewer.xaml.cs          # PDFビューア（WebView2）
```

### Win32 API使用

```csharp
// ウィンドウ配置
[DllImport("user32.dll")]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
    int X, int Y, int cx, int cy, uint uFlags);

// 前面表示
[DllImport("user32.dll")]
private static extern bool SetForegroundWindow(IntPtr hWnd);

// ウィンドウ状態変更
[DllImport("user32.dll")]
private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
```

## 技術仕様

### 動作環境

- **OS**: Windows 10 / 11
- **ランタイム**: .NET 9.0-windows
- **フレームワーク**: WPF (Windows Presentation Foundation)

### 依存パッケージ

- **Microsoft.Web.WebView2** (v1.0.3537.50)
  - PDF表示用WebView2コントロール
  - Edge WebView2 Runtimeが必要

### 画面レイアウト

```
┌─────────────────────────────────────────────────┐
│  ViewerWindow (2/3画面幅)                        │
│  ┌───────────────────────────────────────────┐  │
│  │ Document Viewer - filename.pdf           │  │
│  ├───────────────────────────────────────────┤  │
│  │                                           │  │
│  │  [ImageViewer / TextViewer / PdfViewer]  │  │
│  │                                           │  │
│  │  または                                    │  │
│  │                                           │  │
│  │  外部プログラムウィンドウ（自動配置）         │  │
│  │                                           │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

## トラブルシューティング

### WebView2が初期化できない

**症状**: PDFファイル表示時にエラーが発生
**解決方法**: Edge WebView2 Runtimeをインストール
```bash
# https://developer.microsoft.com/en-us/microsoft-edge/webview2/
```

### Officeファイルのウィンドウが検出できない

**症状**: Excelなどのファイルを開いても120秒後にタイムアウト
**原因**:
- Officeアプリケーションが起動していない
- ウィンドウタイトルにファイル名が含まれていない

**解決方法**:
- Officeアプリケーションが正しくインストールされているか確認
- ファイルが破損していないか確認

### メールファイルが開けない

**症状**: .msgファイルのウィンドウが検出できない
**原因**: Outlookが既定のメールクライアントに設定されていない

**解決方法**:
- Windows設定でOutlookを既定のメールアプリに設定
- Outlookが正しくインストールされているか確認

## 既知の制限事項

1. **Acrobat Reader既存インスタンス**
   - Acrobat Readerで既にPDFを開いている場合、新しいPDFのウィンドウハンドル取得に失敗することがあります
   - 回避策: 内部PDFビューア（WebView2）を使用

2. **CADファイル多様性**
   - CADソフトウェアごとにウィンドウタイトル形式が異なるため、一部環境で検出に失敗する可能性があります

3. **ウィンドウ移動制限**
   - ViewerWindowは移動できません（設計仕様）
   - 必要に応じてコード変更が可能

## ライセンス

MIT License

## 貢献

Issue や Pull Request をお待ちしています。
