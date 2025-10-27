# Document File Manager 使用方法

このドキュメントでは、Document File Manager の基本的な使い方を説明します。

## 目次

- [起動方法](#起動方法)
- [プロジェクト固有DB管理](#プロジェクト固有db管理)
- [基本操作](#基本操作)
- [チェックリスト機能](#チェックリスト機能)
- [資料管理](#資料管理)
- [画面キャプチャ](#画面キャプチャ)
- [設定のカスタマイズ](#設定のカスタマイズ)
- [データ整合性チェック](#データ整合性チェック)

## 起動方法

### 開発時

```bash
# デフォルトパスで起動（開発用）
dotnet run --project src/DocumentFileManager.UI/DocumentFileManager.UI.csproj

# プロジェクトフォルダを指定して起動
dotnet run --project src/DocumentFileManager.UI/DocumentFileManager.UI.csproj -- "C:\Projects\ProjectA\Document"
```

### リリース版

```bash
# プロジェクトフォルダを指定して起動
DocumentFileManager.UI.exe "C:\Projects\ProjectA\Document"

# ショートカットを作成する場合
# リンク先: "C:\Program Files\DocumentFileManager\DocumentFileManager.UI.exe" "C:\Projects\ProjectA\Document"
```

## プロジェクト固有DB管理

Document File Manager は、プロジェクトごとに独立したデータベースとファイルを管理します。

### プロジェクトフォルダの構成

```
C:\Projects\ProjectA\
  └─ Document\              ← documentRootPath（コマンドライン引数で指定）
       ├─ workspace.db      ← プロジェクト固有のSQLiteデータベース
       ├─ checklist.json    ← チェックリスト定義ファイル
       ├─ appsettings.json  ← アプリケーション設定（任意）
       ├─ appsettings.local.json  ← 個人設定（任意、Gitignore対象）
       ├─ plan.pdf          ← 資料ファイル（直接配置）
       ├─ spec.docx         ← 資料ファイル
       ├─ design.xlsx       ← 資料ファイル
       ├─ captures\         ← キャプチャ画像フォルダ
       │   ├─ document_1\
       │   │   └─ capture_20251020_001.png
       │   └─ document_2\
       └─ Logs\             ← アプリケーションログフォルダ
           └─ app-20251020.log
```

### データの独立性

- **プロジェクトA**: `C:\Projects\ProjectA\Document\workspace.db`
- **プロジェクトB**: `C:\Projects\ProjectB\Document\workspace.db`

各プロジェクトは完全に独立しており、データの混在や競合は発生しません。

## 基本操作

### メインウィンドウ

起動すると、以下の要素を持つメインウィンドウが表示されます：

- **資料一覧**: 登録された資料ファイルのリスト
- **資料追加ボタン**: 新しい資料を登録
- **メニューバー**: 設定、データ整合性チェック、終了など

### 資料の登録

1. **資料追加ボタン**をクリック
2. ファイル選択ダイアログで資料ファイルを選択
   - 複数ファイルの同時選択が可能
3. ファイルが documentRootPath 配下にコピーされ、データベースに登録されます

### 資料の閲覧

1. 資料一覧から資料をダブルクリック
2. DocumentFileManager.Viewer が起動し、資料が表示されます
3. サポートされているファイル形式：
   - **内部ビューア**: 画像（PNG, JPG, GIF, BMP）、テキスト（TXT, LOG, CSV, MD）、PDF
   - **外部プログラム**: Office（DOC, DOCX, XLS, XLSX, PPT, PPTX）、CAD（3DM, SLDPRT, DWG, IGS）、メール（MSG, EML）

## チェックリスト機能

### チェックリストウィンドウ

資料をダブルクリックすると、その資料に関連するチェックリストウィンドウが自動的に開きます。

### チェック項目の操作

- **チェックを入れる**: チェックボックスをクリック
  - 状態が「未着手」→「実施中」→「最新」→「未着手」とサイクルします
- **チェック項目の追加**: 「＋」ボタンをクリック
  - 動的にチェック項目を追加できます
  - 追加された項目は checklist.json に自動保存されます

### チェック項目の状態

- **未着手**: 白背景
- **実施中**: 黄色背景
- **最新**: 緑背景
- **改訂**: オレンジ背景

### 画面キャプチャの追加

1. チェック項目を選択
2. **「画面キャプチャ」ボタン**をクリック
3. キャプチャ範囲を選択
4. キャプチャ画像が `captures/document_{id}/` フォルダに保存されます

### キャプチャ画像の表示

- チェック項目の**右側のサムネイル**をクリック
- キャプチャ画像ビューアで拡大表示されます
- ビューア機能：
  - **拡大/縮小**: マウスホイール
  - **画像移動**: ドラッグ
  - **リセット**: リセットボタン

## 資料管理

### 資料の種類

以下のファイル形式をサポート：

**ドキュメント系:**
- PDF: `.pdf`
- Word: `.doc`, `.docx`
- Excel: `.xls`, `.xlsx`, `.xlsm`
- PowerPoint: `.ppt`, `.pptx`

**画像系:**
- `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`

**テキスト系:**
- `.txt`, `.log`, `.csv`, `.md`, `.xml`, `.json`

**CAD系:**
- `.3dm`, `.sldprt`, `.sldasm`, `.dwg`, `.igs`, `.iges`

**メール系:**
- `.msg`, `.eml`

### 資料の検索・フィルタリング

（今後実装予定）

## 画面キャプチャ

### キャプチャの取得

1. チェックリストウィンドウで項目を選択
2. **「画面キャプチャ」ボタン**をクリック
3. 画面全体が暗くなり、キャプチャモードに入ります
4. マウスをドラッグして範囲を選択
5. Enterキーまたはマウスボタンを離すとキャプチャが保存されます
6. Escキーでキャンセル

### キャプチャ画像の保存場所

```
documentRootPath/
  └─ captures/
      └─ document_{documentId}/
          ├─ capture_20251020_001.png
          ├─ capture_20251020_002.png
          └─ capture_20251020_003.png
```

### キャプチャ画像の閲覧

- チェック項目の右側に表示されるサムネイルをクリック
- 専用ビューアで拡大表示
- 拡大/縮小、移動が可能

## 設定のカスタマイズ

### appsettings.json

アプリケーション全体の設定ファイル（チームで共有）：

```json
{
  "PathSettings": {
    "LogsFolder": "logs",
    "DatabaseName": "workspace.db",
    "ConfigDirectory": "config",
    "DocumentsDirectory": "documents",
    "ChecklistFile": "config/checklist.json",
    "SelectedChecklistFile": "config/checklist.json",
    "ChecklistDefinitionsFolder": "",
    "SettingsFile": "appsettings.json",
    "CapturesDirectory": "captures"
  },
  "UISettings": {
    "CheckBox": {
      "MinWidth": 150,
      "FontSize": 14,
      "MarginDepthMultiplier": 10
    },
    "GroupBox": {
      "RootMinWidth": 350,
      "ChildItemMinWidth": 300,
      "Padding": 10
    },
    "Layout": {
      "WrapPanelItemThreshold": 5,
      "MaxColumnsPerRow": 3
    },
    "Colors": {
      "Depth0": { "R": 44, "G": 62, "B": 80 },
      "Depth1": { "R": 52, "G": 152, "B": 219 },
      "Depth2": { "R": 46, "G": 204, "B": 113 }
    }
  }
}
```

### appsettings.local.json

個人設定ファイル（.gitignoreで除外、個人ごとに異なる設定）：

```json
{
  "PathSettings": {
    "SelectedChecklistFile": "my-custom-checklist.json"
  }
}
```

### チェックリストの切り替え

1. メニューバーから**「設定」**を選択
2. **「チェックリスト変更」ボタン**をクリック
3. documentRootPath 配下の `.json` ファイルから選択
4. **「保存」ボタン**をクリック
5. アプリケーションを再起動

## データ整合性チェック

メニューバーから**「データ整合性チェック」**を選択すると、以下のチェックが実行されます：

### チェック項目

1. **見つからない資料ファイル**: DBに登録されているが物理ファイルが存在しない資料
2. **孤立したキャプチャ画像**: どの資料にも関連付けられていないキャプチャ画像

### 修復機能

チェック結果から以下の修復操作が可能：

- **資料の削除**: DBから該当資料を削除
- **キャプチャの削除**: 孤立したキャプチャ画像を削除

## トラブルシューティング

### データベースが見つからない

**症状**: アプリケーション起動時に「workspace.db が見つかりません」というエラー

**解決方法**:
- 正しい documentRootPath を指定して起動しているか確認
- 初回起動時は自動的に workspace.db が作成されます

### ログファイルの確認

問題が発生した場合は、ログファイルを確認してください：

```
documentRootPath/Logs/app-YYYYMMDD.log
```

ログファイルには以下の情報が記録されます：
- アプリケーションの起動・終了
- エラーメッセージとスタックトレース
- ファイル操作の履歴
- データベース操作の履歴

### よくある問題

**Q: 資料ファイルが開けない**

A:
- ファイル形式がサポートされているか確認してください
- 外部プログラム（Word, Excel等）がインストールされているか確認してください
- PDFの場合、Microsoft Edge WebView2 Runtime がインストールされているか確認してください

**Q: キャプチャが保存されない**

A:
- captures フォルダへの書き込み権限があるか確認してください
- ディスク容量が十分にあるか確認してください

**Q: チェックリストが表示されない**

A:
- checklist.json ファイルが documentRootPath 配下に存在するか確認してください
- JSON ファイルの形式が正しいか確認してください

## 親アプリからの呼び出し

NuGet Package化を想定した呼び出し例：

```csharp
using System.Diagnostics;

// プロセスとして起動
var projectDocumentPath = @"C:\Projects\ProjectA\Document";
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "DocumentFileManager.UI.exe",
        Arguments = $"\"{projectDocumentPath}\"",
        UseShellExecute = true
    }
};
process.Start();
```

## さらに詳しい情報

- [README.md](./README.md) - プロジェクト概要
- [FOLDER_STRUCTURE.md](./FOLDER_STRUCTURE.md) - フォルダ構成
- [docs/](./docs/) - 詳細な設計ドキュメント
