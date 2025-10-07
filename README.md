# 資料保存アプリ (Document File Manager)

技術資料（PDF等）とチェックリスト項目を統合管理し、資料と作業進捗を紐づけて可視化するWPFデスクトップアプリケーションです。

## 概要

- **UI**: WPF（Windows Presentation Foundation）ベース
- **データ永続化**: SQLite + Entity Framework Core 8.0
- **対象OS**: Windows 10 / 11
- **ランタイム**: .NET 8

## 主な機能

- 📁 資料ファイル（PDF, DOCX等）の登録・一覧管理
- ✅ 階層構造のチェックリスト定義・状態管理
- 🔗 チェック項目と資料の紐づけ管理
- 📸 画面キャプチャの保存・関連付け
- 🔍 資料の検索・フィルタリング
- 💾 プロジェクト単位でのデータ保存（workspace.db）

## プロジェクト構成

```
DocumentFileManager/
├── src/
│   ├── DocumentFileManager/              # ドメイン層（エンティティ・値オブジェクト）
│   │   ├── Entities/
│   │   │   ├── CheckItem.cs             # チェック項目
│   │   │   ├── Document.cs              # 資料ファイル
│   │   │   └── CheckItemDocument.cs     # 紐づけ管理
│   │   └── ValueObjects/
│   │       └── ItemStatus.cs            # 状態列挙型
│   └── DocumentFileManager.Infrastructure/  # インフラ層（DB・リポジトリ）
│       ├── Data/
│       │   ├── DocumentManagerContext.cs     # EF Core DbContext
│       │   └── DocumentManagerContextFactory.cs
│       ├── Repositories/
│       │   ├── ICheckItemRepository.cs
│       │   ├── CheckItemRepository.cs
│       │   ├── IDocumentRepository.cs
│       │   ├── DocumentRepository.cs
│       │   ├── ICheckItemDocumentRepository.cs
│       │   └── CheckItemDocumentRepository.cs
│       └── Migrations/                   # EF Core マイグレーション
├── tests/
│   └── DocumentFileManager.UI.Test/      # UI テストプロジェクト（WPF）
├── docs/                                 # 設計ドキュメント
│   ├── 要件定義.md
│   ├── ユースケース.md
│   ├── ドメインモデル定義書.md
│   ├── データ設計書.md
│   └── 実装プラン.md
└── dummy/                                # テスト用ダミーデータ
    ├── 設計書_rev1.pdf
    ├── 仕様書_最新版.pdf
    ├── テスト計画書.docx
    └── picture/
        ├── capture_001.png
        ├── capture_002.png
        └── screenshot_20251007.png
```

## 開発環境セットアップ

### 必要環境

- .NET SDK 8.0 以上
- Visual Studio 2022 または Visual Studio Code
- SQLite（.NET SDK に含まれる）

### ビルド手順

```bash
# リポジトリクローン
git clone <repository-url>
cd DocumentFileManager

# 依存関係の復元
dotnet restore

# ビルド
dotnet build

# EF Core ツールのインストール（初回のみ）
dotnet tool install --global dotnet-ef

# データベースマイグレーション適用
cd src/DocumentFileManager.Infrastructure
dotnet ef database update
```

## テストデータ

開発・テスト用のダミーファイルが `dummy/` フォルダに用意されています。

### ダミーファイル一覧

**資料ファイル（`dummy/`）:**
- `設計書_rev1.pdf` - PDF形式の設計書サンプル
- `仕様書_最新版.pdf` - PDF形式の仕様書サンプル
- `テスト計画書.docx` - Word形式のドキュメントサンプル

**キャプチャ画像（`dummy/picture/`）:**
- `capture_001.png` - 画面キャプチャサンプル1
- `capture_002.png` - 画面キャプチャサンプル2
- `screenshot_20251007.png` - スクリーンショットサンプル

### テストデータの使用方法

これらのダミーファイルは以下の用途で使用します：

1. **Document エンティティのテスト**: 資料登録・一覧表示機能の動作確認
2. **CheckItemDocument のテスト**: チェック項目と資料の紐づけ機能確認
3. **相対パス管理のテスト**: プロジェクト可搬性の検証
4. **画面キャプチャ機能のテスト**: CaptureFile プロパティの動作確認

## データベース構造

SQLite データベース（`workspace.db`）に以下のテーブルを作成：

### CheckItems テーブル
- チェックリスト項目を階層構造で管理
- 自己参照外部キー（ParentId）で親子関係を表現
- Path 列で階層パス識別（例: "設計図面/平面図"）

### Documents テーブル
- 資料ファイルのメタデータを管理
- 相対パスでプロジェクト可搬性を確保

### CheckItemDocuments テーブル
- チェック項目と資料の多対多関係を管理
- 紐づけ日時（LinkedAt）で履歴管理
- キャプチャファイルパス（CaptureFile）を保存

詳細は [docs/データ設計書.md](./docs/データ設計書.md) を参照してください。

## 設計ドキュメント

- [要件定義.md](./docs/要件定義.md) - システム概要・機能要件・非機能要件
- [ユースケース.md](./docs/ユースケース.md) - 利用シナリオ・処理フロー
- [ドメインモデル定義書.md](./docs/ドメインモデル定義書.md) - エンティティ設計
- [データ設計書.md](./docs/データ設計書.md) - テーブル定義・ER図
- [実装プラン.md](./docs/実装プラン.md) - 開発タスク・マイルストーン

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

## 開発状況

### 完了
- ✅ ドメインモデル設計・実装
- ✅ データベーススキーマ設計
- ✅ EF Core マイグレーション作成
- ✅ リポジトリパターン実装
- ✅ テストデータ作成

### 進行中
- 🔄 依存性注入（DI）設定
- 🔄 WPF UI 実装

### 未着手
- ⬜ アプリケーション層（サービス）実装
- ⬜ 画面キャプチャ機能実装
- ⬜ ユニットテスト作成
- ⬜ 統合テスト作成

## ライセンス

MIT License

## 貢献

Issue や Pull Request をお待ちしています。
