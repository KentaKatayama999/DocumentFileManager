# 資料保存アプリ (Document File Manager)

[![CI/CD Pipeline](https://github.com/KentaKatayama999/DocumentFileManager/actions/workflows/ci.yml/badge.svg)](https://github.com/KentaKatayama999/DocumentFileManager/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/KentaKatayama999/DocumentFileManager/branch/main/graph/badge.svg)](https://codecov.io/gh/KentaKatayama999/DocumentFileManager)

技術資料（PDF等）とチェックリスト項目を統合管理し、資料と作業進捗を紐づけて可視化するWPFデスクトップアプリケーションです。

## 概要

- **UI**: WPF（Windows Presentation Foundation）ベース
- **データ永続化**: SQLite + Entity Framework Core 8.0
- **対象OS**: Windows 10 / 11
- **ランタイム**: .NET 9.0 (UI/Viewer), .NET 8.0 (Infrastructure)
- **アーキテクチャ**: プロジェクト固有DB管理、NuGet Package化対応

## 主な機能

- 📁 資料ファイル（PDF, DOCX等）の登録・一覧管理
- ✅ 階層構造のチェックリスト定義・状態管理
- 🔗 チェック項目と資料の紐づけ管理
- 📸 画面キャプチャの保存・関連付け
- 🔍 資料の検索・フィルタリング
- 💾 **プロジェクト固有のデータ管理**（workspace.db、ログ、キャプチャをプロジェクトフォルダ内に配置）
- 👁️ 統合ドキュメントビューア（画像、PDF、テキスト、Office、CAD、メールファイル対応）
- 🎯 チェックリスト選択機能（複数のチェックリスト定義を切り替え可能）
- 📊 データ整合性チェック機能
- 🎨 UI設定のカスタマイズ（外部JSON設定）
- 🔐 個人設定対応（appsettings.local.json）

## 新機能：プロジェクト固有DB管理

コマンドライン引数でプロジェクトのドキュメントルートパスを指定することで、各プロジェクトごとに独立したデータベースとファイルを管理できます。

### プロジェクトフォルダ構成例

```
C:\Projects\ProjectA\
  └─ Document\              ← documentRootPath (コマンドライン引数で指定)
       ├─ workspace.db      ← プロジェクト固有のDB
       ├─ checklist.json    ← チェックリスト定義
       ├─ appsettings.json  ← アプリ設定
       ├─ appsettings.local.json  ← 個人設定（.gitignoreに追加済み）
       ├─ plan.pdf          ← 資料ファイル
       ├─ spec.docx
       ├─ captures\         ← キャプチャ画像
       └─ Logs\             ← アプリケーションログ
```

### 使用方法

```bash
# コマンドライン引数でプロジェクトフォルダを指定
DocumentFileManager.UI.exe "C:\Projects\ProjectA\Document"

# または開発時
dotnet run --project src/DocumentFileManager.UI/DocumentFileManager.UI.csproj -- "C:\Projects\ProjectA\Document"

# 引数なしの場合はデフォルトパス（開発用）を使用
dotnet run --project src/DocumentFileManager.UI/DocumentFileManager.UI.csproj
```

詳細は [USAGE.md](./USAGE.md) を参照してください。

## プロジェクト構成

```
DocumentFileManager/
├── src/
│   ├── DocumentFileManager/              # ドメイン層（エンティティ・値オブジェクト）
│   ├── DocumentFileManager.Infrastructure/  # インフラ層（DB・リポジトリ）
│   ├── DocumentFileManager.UI/           # メインアプリケーション（WPF）
│   └── DocumentFileManager.Viewer/       # ドキュメントビューア（スタンドアロン）
├── tests/                                # テストプロジェクト
├── docs/                                 # 設計ドキュメント
└── test-files/                           # ビューアテスト用ファイル
```

詳細は [FOLDER_STRUCTURE.md](./FOLDER_STRUCTURE.md) を参照してください。

## 開発環境セットアップ

### 必要環境

- .NET SDK 9.0 以上
- Visual Studio 2022 または Visual Studio Code
- SQLite（.NET SDK に含まれる）
- Microsoft Edge WebView2 Runtime（PDFビューア用）

### ビルド手順

```bash
# リポジトリクローン
git clone https://github.com/KentaKatayama999/DocumentFileManager.git
cd DocumentFileManager

# 依存関係の復元
dotnet restore

# ビルド
dotnet build

# EF Core ツールのインストール（初回のみ）
dotnet tool install --global dotnet-ef

# UIアプリケーション実行
dotnet run --project src/DocumentFileManager.UI/DocumentFileManager.UI.csproj
```

データベースマイグレーションは初回起動時に自動適用されます。

## 設定ファイル

### appsettings.json

アプリケーション全体の設定ファイル（リポジトリにコミット）

```json
{
  "PathSettings": {
    "LogsFolder": "Logs",
    "DatabaseName": "workspace.db",
    "ChecklistFile": "checklist.json",
    "SelectedChecklistFile": "checklist.json",
    "CapturesDirectory": "captures"
  },
  "UISettings": {
    "CheckBox": { ... },
    "GroupBox": { ... },
    "Layout": { ... },
    "Colors": { ... }
  }
}
```

### appsettings.local.json（個人設定）

個人ごとの設定ファイル（.gitignoreで除外、コミットされない）

```json
{
  "PathSettings": {
    "SelectedChecklistFile": "my-custom-checklist.json"
  }
}
```

## DocumentFileManager.Viewer

統合ドキュメントビューアは、様々なファイル形式をシームレスに表示するスタンドアロンアプリケーションです。

### 対応ファイル形式

**内部ビューア表示:**
- 📷 画像ファイル: `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`
- 📝 テキストファイル: `.txt`, `.log`, `.csv`, `.md`, `.xml`, `.json`
- 📄 PDFファイル: `.pdf` (WebView2使用)

**外部プログラム連携（自動ウィンドウ配置）:**
- 📊 Officeファイル: `.doc`, `.docx`, `.xls`, `.xlsx`, `.xlsm`, `.ppt`, `.pptx`
- 🔧 CADファイル: `.3dm`, `.sldprt`, `.sldasm`, `.dwg`, `.igs`, `.iges`
- ✉️ メールファイル: `.msg`, `.eml`

### 特徴

- **自動ウィンドウ配置**: 外部プログラムで開いたファイルを画面左2/3に自動配置
- **ウィンドウハンドル取得**: 開いたファイルのウィンドウハンドルをイベントで通知
- **固定ウィンドウ**: ビューアウィンドウは画面左端に固定（移動不可）
- **シームレス統合**: MainWindowから直接呼び出し可能

## データベース構造

SQLite データベース（`workspace.db`）に以下のテーブルを作成：

### CheckItems テーブル
- チェックリスト項目を階層構造で管理
- 自己参照外部キー（ParentId）で親子関係を表現
- Path 列で階層パス識別（例: "設計図面/平面図"）

### Documents テーブル
- 資料ファイルのメタデータを管理
- **RelativePath: ファイル名のみを保存**（プロジェクト可搬性確保）

### CheckItemDocuments テーブル
- チェック項目と資料の多対多関係を管理
- 紐づけ日時（LinkedAt）で履歴管理
- キャプチャファイルパス（CaptureFile）を保存

詳細は [docs/データ設計書.md](./docs/データ設計書.md) を参照してください。

## アーキテクチャ

### レイヤー構成

```
┌─────────────────────────────┐
│   UI Layer (WPF)            │  ← 画面表示・ユーザー操作
├─────────────────────────────┤
│   Application Layer         │  ← ユースケース・ビジネスロジック
├─────────────────────────────┤
│   Domain Layer              │  ← エンティティ・ドメインルール
├─────────────────────────────┤
│   Infrastructure Layer      │  ← DB アクセス・外部連携
└─────────────────────────────┘
```

### 設計原則

- **ドメイン駆動設計**: ビジネスルールをドメイン層に集約
- **リポジトリパターン**: データアクセスを抽象化
- **依存性注入**: インターフェースベースの疎結合設計
- **MVVM パターン**: UI とロジックの分離（WPF）
- **プロジェクト固有DB**: データの独立性と可搬性を確保

## 開発状況

### 完了
- ✅ ドメインモデル設計・実装
- ✅ データベーススキーマ設計
- ✅ EF Core マイグレーション作成
- ✅ リポジトリパターン実装
- ✅ DocumentFileManager.Viewer 実装
- ✅ 依存性注入（DI）設定
- ✅ WPF UI 実装
- ✅ チェックリスト選択機能
- ✅ データ整合性チェック機能
- ✅ 画面キャプチャ機能
- ✅ 設定外部化（appsettings.json）
- ✅ プロジェクト固有DB管理
- ✅ コマンドライン引数対応
- ✅ 個人設定対応（appsettings.local.json）

### 進行中
- 🔄 テスト実装

### 未着手
- ⬜ NuGet Package化
- ⬜ CI/CD パイプライン整備
- ⬜ パフォーマンス最適化

## ドキュメント

- [USAGE.md](./USAGE.md) - 使用方法・操作ガイド
- [FOLDER_STRUCTURE.md](./FOLDER_STRUCTURE.md) - フォルダ構成詳細
- [docs/要件定義.md](./docs/要件定義.md) - システム概要・機能要件
- [docs/ユースケース.md](./docs/ユースケース.md) - 利用シナリオ
- [docs/ドメインモデル定義書.md](./docs/ドメインモデル定義書.md) - エンティティ設計
- [docs/データ設計書.md](./docs/データ設計書.md) - テーブル定義・ER図
- [docs/実装プラン.md](./docs/実装プラン.md) - 開発タスク

## ライセンス

MIT License

## 貢献

Issue や Pull Request をお待ちしています。

## 作者

Kenta Katayama ([@KentaKatayama999](https://github.com/KentaKatayama999))
