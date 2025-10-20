# Document File Manager フォルダ構成

このドキュメントでは、Document File Manager のフォルダ構成と各ファイルの役割を詳細に説明します。

## 目次

- [プロジェクト全体の構成](#プロジェクト全体の構成)
- [src/ - ソースコード](#src---ソースコード)
- [tests/ - テストコード](#tests---テストコード)
- [docs/ - ドキュメント](#docs---ドキュメント)
- [プロジェクトデータフォルダ](#プロジェクトデータフォルダ)

## プロジェクト全体の構成

```
DocumentFileManager/                 # リポジトリルート
├── .claude/                         # Claude Code 設定
│   └── settings.local.json         # ローカル設定
├── .github/                         # GitHub Actions ワークフロー
│   └── workflows/
│       └── ci.yml                  # CI/CD パイプライン
├── .vs/                            # Visual Studio 設定（.gitignore対象）
├── src/                            # ソースコードディレクトリ
│   ├── DocumentFileManager/        # ドメイン層
│   ├── DocumentFileManager.Infrastructure/  # インフラ層
│   ├── DocumentFileManager.UI/     # UIアプリケーション
│   └── DocumentFileManager.Viewer/ # ドキュメントビューア
├── tests/                          # テストプロジェクト
│   └── DocumentFileManager.UI.Test/
├── docs/                           # 設計ドキュメント
│   ├── 要件定義.md
│   ├── ユースケース.md
│   ├── ドメインモデル定義書.md
│   ├── データ設計書.md
│   └── 実装プラン.md
├── test-files/                     # ビューアテスト用サンプルファイル
│   ├── images/                     # 画像ファイル
│   ├── text/                       # テキストファイル
│   ├── pdf/                        # PDFファイル
│   ├── office/                     # Officeファイル
│   ├── cad/                        # CADファイル
│   └── email/                      # メールファイル
├── dummy/                          # 開発テスト用ダミーデータ
│   └── picture/
├── .gitignore                      # Git除外設定
├── .gitattributes                  # Git属性設定
├── .editorconfig                   # エディタ設定
├── Directory.Build.props           # MSBuild共通設定
├── DocumentFileManager.sln         # ソリューションファイル
├── README.md                       # プロジェクト概要
├── USAGE.md                        # 使用方法
├── FOLDER_STRUCTURE.md             # このファイル
└── PROGRESS.md                     # 開発進捗記録
```

## src/ - ソースコード

### DocumentFileManager/ - ドメイン層

```
src/DocumentFileManager/
├── Entities/                       # エンティティ
│   ├── CheckItem.cs               # チェック項目エンティティ
│   ├── Document.cs                # 資料エンティティ
│   └── CheckItemDocument.cs       # 紐づけエンティティ（多対多）
├── ValueObjects/                   # 値オブジェクト
│   └── ItemStatus.cs              # チェック項目の状態列挙型
└── DocumentFileManager.csproj     # プロジェクトファイル
```

**役割**:
- ビジネスロジックの中核となるドメインモデルを定義
- インフラやUIに依存しない純粋なビジネスルール

**主要クラス**:

**CheckItem.cs**
- Id: 主キー
- Label: 表示名
- Path: 階層パス（例: "設計/図面/平面図"）
- Depth: 階層の深さ
- Order: 表示順序
- Status: 状態（未着手、実施中、最新、改訂）
- ParentId: 親項目のID（自己参照外部キー）
- CreatedAt: 作成日時
- UpdatedAt: 更新日時

**Document.cs**
- Id: 主キー
- FileName: ファイル名
- RelativePath: 相対パス（ファイル名のみ保存）
- FileSize: ファイルサイズ
- Extension: 拡張子
- CreatedAt: 作成日時
- UpdatedAt: 更新日時

**CheckItemDocument.cs**
- Id: 主キー
- CheckItemId: チェック項目ID
- DocumentId: 資料ID
- LinkedAt: 紐づけ日時
- CaptureFile: キャプチャ画像ファイルパス

### DocumentFileManager.Infrastructure/ - インフラ層

```
src/DocumentFileManager.Infrastructure/
├── Data/                           # データアクセス
│   ├── DocumentManagerContext.cs  # EF Core DbContext
│   ├── DocumentManagerContextFactory.cs  # デザインタイムファクトリ
│   └── DataSeeder.cs              # 初期データシード
├── Migrations/                     # EF Core マイグレーション
│   ├── 20250101000000_InitialCreate.cs
│   └── 20250101000000_InitialCreate.Designer.cs
├── Repositories/                   # リポジトリ実装
│   ├── ICheckItemRepository.cs    # チェック項目リポジトリインターフェース
│   ├── CheckItemRepository.cs     # チェック項目リポジトリ実装
│   ├── IDocumentRepository.cs     # 資料リポジトリインターフェース
│   ├── DocumentRepository.cs      # 資料リポジトリ実装
│   ├── ICheckItemDocumentRepository.cs  # 紐づけリポジトリインターフェース
│   └── CheckItemDocumentRepository.cs   # 紐づけリポジトリ実装
├── Services/                       # インフラサービス
│   ├── ChecklistLoader.cs         # checklist.json読み込み
│   └── ChecklistSaver.cs          # checklist.json保存
├── Models/                         # インフラモデル
│   └── CheckItemDefinition.cs     # JSON読み込み用モデル
└── DocumentFileManager.Infrastructure.csproj
```

**役割**:
- データベースアクセスの実装
- Entity Framework Core によるO/Rマッピング
- リポジトリパターンによるデータアクセスの抽象化

**主要機能**:
- SQLiteデータベースとの接続
- マイグレーションによるスキーマ管理
- checklist.jsonの読み込み・保存

### DocumentFileManager.UI/ - UIアプリケーション

```
src/DocumentFileManager.UI/
├── Configuration/                  # 設定クラス
│   ├── PathSettings.cs            # パス設定
│   └── UISettings.cs              # UI設定
├── Helpers/                        # ヘルパークラス
│   └── CheckItemUIBuilder.cs      # チェックリストUI構築ヘルパー
├── Services/                       # アプリケーションサービス
│   ├── IDataIntegrityService.cs   # データ整合性サービスインターフェース
│   ├── DataIntegrityService.cs    # データ整合性サービス実装
│   └── SettingsPersistence.cs     # 設定永続化サービス
├── Models/                         # UIモデル
│   └── IntegrityReport.cs         # 整合性チェックレポート
├── App.xaml                        # アプリケーション定義
├── App.xaml.cs                     # アプリケーションロジック
│   └── ※DI設定、Serilog設定、コマンドライン引数処理
├── MainWindow.xaml                 # メインウィンドウUI
├── MainWindow.xaml.cs              # メインウィンドウロジック
├── ChecklistWindow.xaml            # チェックリストウィンドウUI
├── ChecklistWindow.xaml.cs         # チェックリストウィンドウロジック
├── SettingsWindow.xaml             # 設定ウィンドウUI
├── SettingsWindow.xaml.cs          # 設定ウィンドウロジック
├── IntegrityReportWindow.xaml      # 整合性レポートウィンドウUI
├── IntegrityReportWindow.xaml.cs   # 整合性レポートウィンドウロジック
├── ChecklistSelectionDialog.xaml  # チェックリスト選択ダイアログUI
├── ChecklistSelectionDialog.xaml.cs # チェックリスト選択ダイアログロジック
├── ScreenCaptureOverlay.xaml       # 画面キャプチャオーバーレイUI
├── ScreenCaptureOverlay.xaml.cs    # 画面キャプチャオーバーレイロジック
├── CaptureImageViewerWindow.xaml   # キャプチャ画像ビューアUI
├── CaptureImageViewerWindow.xaml.cs # キャプチャ画像ビューアロジック
├── ImagePreviewWindow.xaml         # 画像プレビューUI
├── ImagePreviewWindow.xaml.cs      # 画像プレビューロジック
├── appsettings.json                # アプリケーション設定
└── DocumentFileManager.UI.csproj   # プロジェクトファイル
```

**役割**:
- WPFベースのユーザーインターフェース
- 依存性注入（DI）によるサービス管理
- Serilogによるロギング
- プロジェクト固有DB管理

**主要機能**:
- 資料の登録・一覧表示
- チェックリストの表示・編集
- 画面キャプチャ機能
- データ整合性チェック
- 設定管理

### DocumentFileManager.Viewer/ - ドキュメントビューア

```
src/DocumentFileManager.Viewer/
├── Viewers/                        # ビューアコントロール
│   ├── ImageViewer.xaml           # 画像ビューア
│   ├── ImageViewer.xaml.cs
│   ├── TextViewer.xaml            # テキストビューア
│   ├── TextViewer.xaml.cs
│   ├── PdfViewer.xaml             # PDFビューア（WebView2）
│   └── PdfViewer.xaml.cs
├── App.xaml                        # アプリケーション定義
├── App.xaml.cs                     # アプリケーションロジック
├── ViewerWindow.xaml               # ビューアメインウィンドウUI
├── ViewerWindow.xaml.cs            # ビューアメインウィンドウロジック
├── MainWindow.xaml                 # スタンドアロン起動用ウィンドウ
├── MainWindow.xaml.cs              # スタンドアロン起動用ロジック
└── DocumentFileManager.Viewer.csproj
```

**役割**:
- 様々なファイル形式の統合ビューア
- 内部ビューア（画像、テキスト、PDF）
- 外部プログラム連携（Office、CAD、メール）
- 自動ウィンドウ配置

**対応ファイル形式**:
- 画像: PNG, JPG, GIF, BMP
- テキスト: TXT, LOG, CSV, MD, XML, JSON
- PDF: PDF（WebView2使用）
- Office: DOC, DOCX, XLS, XLSX, XLSM, PPT, PPTX
- CAD: 3DM, SLDPRT, SLDASM, DWG, IGS, IGES
- メール: MSG, EML

## tests/ - テストコード

```
tests/
└── DocumentFileManager.UI.Test/    # UIテストプロジェクト
    ├── CheckItemTests.cs           # CheckItemエンティティテスト
    ├── DocumentTests.cs            # Documentエンティティテスト
    ├── CheckItemDocumentTests.cs   # CheckItemDocumentエンティティテスト
    └── DocumentFileManager.UI.Test.csproj
```

**役割**:
- ユニットテスト
- 統合テスト
- リグレッションテスト

## docs/ - ドキュメント

```
docs/
├── 要件定義.md                     # 機能要件・非機能要件
├── ユースケース.md                 # ユースケース図・シナリオ
├── ドメインモデル定義書.md         # エンティティ定義
├── データ設計書.md                 # テーブル定義・ER図
└── 実装プラン.md                   # 開発タスク・マイルストーン
```

**役割**:
- 設計ドキュメントの管理
- 要件・仕様の記録
- 開発ガイドライン

## プロジェクトデータフォルダ

各プロジェクトの documentRootPath 配下には、以下のファイル・フォルダが配置されます。

```
documentRootPath/                   # コマンドライン引数で指定
├── workspace.db                    # プロジェクト固有のSQLiteデータベース
├── checklist.json                  # チェックリスト定義ファイル
├── appsettings.json                # プロジェクト固有のアプリ設定（任意）
├── appsettings.local.json          # 個人設定（.gitignore対象、任意）
├── *.pdf                           # 資料ファイル（直接配置）
├── *.docx                          # 資料ファイル
├── *.xlsx                          # 資料ファイル
├── captures/                       # キャプチャ画像フォルダ
│   ├── document_1/                # 資料ID=1のキャプチャ
│   │   ├── capture_20251020_001.png
│   │   └── capture_20251020_002.png
│   └── document_2/                # 資料ID=2のキャプチャ
│       └── capture_20251020_001.png
└── Logs/                           # ログフォルダ
    ├── app-20251020.log           # 日付ごとのログファイル
    └── app-20251021.log
```

### workspace.db

**形式**: SQLite データベース
**内容**:
- CheckItems テーブル: チェック項目
- Documents テーブル: 資料メタデータ
- CheckItemDocuments テーブル: 紐づけ関係

### checklist.json

**形式**: JSON
**内容**: チェックリストの階層構造定義

```json
{
  "checkItems": [
    {
      "label": "設計",
      "type": "category",
      "children": [
        {
          "label": "図面",
          "type": "category",
          "children": [
            {
              "label": "平面図",
              "type": "item",
              "checked": true
            }
          ]
        }
      ]
    }
  ]
}
```

### captures/

**形式**: PNG画像ファイル
**命名規則**: `capture_YYYYMMDD_NNN.png`
**保存場所**: `captures/document_{documentId}/`

### Logs/

**形式**: テキストログファイル
**命名規則**: `app-YYYYMMDD.log`
**ローテーション**: 日次

## ファイル命名規則

### キャプチャ画像

```
capture_YYYYMMDD_NNN.png

例:
capture_20251020_001.png
capture_20251020_002.png
```

### ログファイル

```
app-YYYYMMDD.log

例:
app-20251020.log
app-20251021.log
```

### 資料ファイル

```
元のファイル名をそのまま使用
重複時は連番を追加: {filename}_{N}.{ext}

例:
plan.pdf
spec.docx
design.xlsx
plan_1.pdf  # 重複時
```

## データの独立性

プロジェクトごとに完全に独立したデータ管理：

```
C:\Projects\
├── ProjectA\
│   └── Document\
│       ├── workspace.db        # ProjectA専用DB
│       ├── checklist.json
│       ├── captures\
│       └── Logs\
└── ProjectB\
    └── Document\
        ├── workspace.db        # ProjectB専用DB（独立）
        ├── checklist.json
        ├── captures\
        └── Logs\
```

各プロジェクトは独立しており、データの混在や競合は発生しません。

## 参考資料

- [README.md](./README.md) - プロジェクト概要
- [USAGE.md](./USAGE.md) - 使用方法
- [docs/](./docs/) - 詳細な設計ドキュメント
