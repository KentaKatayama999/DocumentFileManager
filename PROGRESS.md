# 資料保存アプリ 開発進捗管理

**最終更新日**: 2025-10-07（設定外部化とJSON同期機能の実装完了）

## プロジェクト概要

- **目的**: 技術資料とチェックリストを統合管理し、資料と作業進捗を紐づけて可視化する
- **技術スタック**: WPF, .NET 8, SQLite, EF Core 8.0, Serilog
- **アーキテクチャ**: ドメイン駆動設計、リポジトリパターン、依存性注入

---

## 全体タスク一覧

### フェーズ1: 基盤整備 ✅ **完了**

| タスクID | タスク名 | 状態 | 完了日 | 備考 |
|---------|---------|------|--------|------|
| INFRA-01 | ドメインモデル設計 | ✅ 完了 | 2025-10-07 | CheckItem, Document, CheckItemDocument |
| INFRA-02 | データベース設計（SQLite） | ✅ 完了 | 2025-10-07 | テーブル定義、ER図、外部キー制約 |
| INFRA-03 | EF Core マイグレーション | ✅ 完了 | 2025-10-07 | InitialCreate マイグレーション作成・適用 |
| INFRA-04 | リポジトリパターン実装 | ✅ 完了 | 2025-10-07 | 3リポジトリ（CheckItem, Document, CheckItemDocument） |
| INFRA-05 | 依存性注入（DI）設定 | ✅ 完了 | 2025-10-07 | App.xaml.cs でサービス登録 |
| INFRA-06 | ロギング基盤（Serilog） | ✅ 完了 | 2025-10-07 | ファイル・コンソール出力、グローバル例外ハンドラ |

**成果物:**
- `src/DocumentFileManager/Entities/` - ドメインエンティティ（4ファイル）
- `src/DocumentFileManager.Infrastructure/Data/` - DbContext, マイグレーション
- `src/DocumentFileManager.Infrastructure/Repositories/` - リポジトリ（6ファイル）
- `Logs/app-YYYYMMDD.log` - ログファイル（日次ローテーション）

---

### フェーズ2: データ整備 ✅ **完了**

| タスクID | タスク名 | 状態 | 完了日 | 備考 |
|---------|---------|------|--------|------|
| DATA-01 | ダミーファイル作成 | ✅ 完了 | 2025-10-07 | dummy/ フォルダ（資料3件、画像3件） |
| DATA-02 | シードデータ投入機能 | ✅ 完了 | 2025-10-07 | DataSeeder クラス、起動時自動投入 |
| DATA-03 | JSON設定ファイル対応 | ✅ 完了 | 2025-10-07 | checklist.json、ChecklistLoader |
| DATA-04 | 階層構造データ生成 | ✅ 完了 | 2025-10-07 | 4階層（大分類/中分類/小分類/項目） |

**成果物:**
- `checklist.json` - チェック項目定義ファイル
- `src/DocumentFileManager.Infrastructure/Data/DataSeeder.cs` - シードデータ投入
- `src/DocumentFileManager.Infrastructure/Services/ChecklistLoader.cs` - JSON読み込み
- `src/DocumentFileManager.Infrastructure/Models/CheckItemDefinition.cs` - JSONモデル
- `dummy/` - テスト用ダミーファイル（資料3件、画像3件）

**データ統計:**
- チェック項目: 分類=18件、項目=15件、チェック済み=8件（checklist.json と同期）
- 資料ファイル: 3件（PDF×2, DOCX×1）
- 紐づけデータ: 3件（キャプチャ付き2件）

**JSON-DB同期:**
- checklist.json をマスターデータ化（ファイル差し替えで初期定義変更可能）
- 起動時に毎回同期、ユーザーのチェック状態と資料紐づけは保持

---

### フェーズ3: UI実装 🔄 **進行中**

| タスクID | タスク名 | 状態 | 完了日 | 備考 |
|---------|---------|------|--------|------|
| UI-01 | メインウィンドウ基本構成 | ✅ 完了 | 2025-10-07 | タイトルバー、ステータスバー、2分割レイアウト |
| UI-02 | 資料一覧表示（ListView） | ✅ 完了 | 2025-10-07 | 資料読み込みボタン、GridView表示 |
| UI-03 | チェック項目一覧表示（ListView） | ✅ 完了 | 2025-10-07 | チェック項目読み込みボタン、GridView表示 |
| UI-04 | TreeView階層表示 | 🔄 進行中 | - | チェック項目をTreeView表示 |
| UI-05 | チェックボックス機能 | ⬜ 未着手 | - | チェック状態の切り替え（Current/Unspecified） |
| UI-06 | 資料登録UI | ⬜ 未着手 | - | ファイル選択ダイアログ、ドラッグ&ドロップ |
| UI-07 | チェック項目登録UI | ⬜ 未着手 | - | ツリー構造での追加・編集 |
| UI-08 | 紐づけUI | ⬜ 未着手 | - | 資料とチェック項目の紐づけ・解除 |
| UI-09 | 紐づけ一覧表示 | ⬜ 未着手 | - | 紐づけ済みデータの表示 |
| UI-10 | 画面キャプチャ機能 | ⬜ 未着手 | - | スクリーンショット取得・保存 |
| UI-11 | UI設定外部化とパス管理 | ✅ 完了 | 2025-10-07 | appsettings.json、PathSettings、UISettings |
| UI-12 | 設定ウィンドウ実装 | ✅ 完了 | 2025-10-07 | SettingsWindow、入力値検証、JSON永続化 |

**成果物（現在）:**
- `src/DocumentFileManager.UI/MainWindow.xaml(.cs)` - メインウィンドウ（メニューバー追加）
- `src/DocumentFileManager.UI/SettingsWindow.xaml(.cs)` - 設定ウィンドウ（UI設定変更）
- `src/DocumentFileManager.UI/App.xaml.cs` - DI設定、Serilog設定、PathSettings統合
- `src/DocumentFileManager.UI/Configuration/PathSettings.cs` - パス設定クラス
- `src/DocumentFileManager.Infrastructure/Data/DataSeeder.cs` - JSON-DB同期機能
- `appsettings.json` - 統合設定ファイル（PathSettings、UISettings）
- `docs/2025-10-07_設定外部化とJSON同期の実装.md` - 実装ドキュメント

**次の作業:**
- TreeView表示への変更
- チェックボックス機能の実装
- 資料登録機能の追加

---

### フェーズ4: 機能強化 ⬜ **未着手**

| タスクID | タスク名 | 状態 | 完了日 | 備考 |
|---------|---------|------|--------|------|
| FEAT-01 | ファイル存在検証 | ⬜ 未着手 | - | 起動時・定期的なファイルチェック |
| FEAT-02 | データバックアップ機能 | ⬜ 未着手 | - | workspace.db の手動・自動バックアップ |
| FEAT-03 | 資料検索機能 | ⬜ 未着手 | - | ファイル名・ファイルタイプでの検索 |
| FEAT-04 | チェック項目フィルタ | ⬜ 未着手 | - | 状態（Current/Revised等）でのフィルタ |
| FEAT-05 | エクスポート機能 | ⬜ 未着手 | - | Excel, CSV, PDF等への出力 |
| FEAT-06 | 履歴管理 | ⬜ 未着手 | - | チェック状態変更履歴の記録・表示 |

---

### フェーズ5: テスト・品質保証 ⬜ **未着手**

| タスクID | タスク名 | 状態 | 完了日 | 備考 |
|---------|---------|------|--------|------|
| TEST-01 | ユニットテスト（ドメイン層） | ⬜ 未着手 | - | CheckItem, Document エンティティのテスト |
| TEST-02 | ユニットテスト（リポジトリ層） | ⬜ 未着手 | - | CRUD操作のテスト |
| TEST-03 | 統合テスト（主要ユースケース） | ⬜ 未着手 | - | 資料登録、チェック項目管理、紐づけ |
| TEST-04 | UI自動テスト | ⬜ 未着手 | - | ボタンクリック、画面遷移のテスト |
| TEST-05 | パフォーマンステスト | ⬜ 未着手 | - | 大量データでの動作確認 |

---

### フェーズ6: ドキュメント・運用 ⬜ **未着手**

| タスクID | タスク名 | 状態 | 完了日 | 備考 |
|---------|---------|------|--------|------|
| DOC-01 | ユーザーマニュアル作成 | ⬜ 未着手 | - | 操作手順、画面キャプチャ付き |
| DOC-02 | 開発者向けドキュメント | ⬜ 未着手 | - | アーキテクチャ図、クラス図 |
| DOC-03 | 運用手順書 | ⬜ 未着手 | - | バックアップ、トラブルシューティング |
| DOC-04 | データ移行ツール | ⬜ 未着手 | - | 旧XML形式からの移行スクリプト |

---

## プロジェクト統計

### コード行数（概算）
- ドメイン層: ~300行
- インフラ層: ~800行
- UI層: ~200行
- 合計: ~1,300行

### ファイル数
- C# ソースファイル: 20ファイル
- XAML ファイル: 3ファイル
- 設定ファイル: 3ファイル（checklist.json, appsettings.json等）
- ドキュメント: 6ファイル

### データベース
- テーブル数: 3テーブル（CheckItems, Documents, CheckItemDocuments）
- インデックス数: 6個
- 外部キー制約: 3個

---

## 既知の課題・技術的負債

### 優先度：高
- なし（現時点で重大な問題なし）

### 優先度：中
- [ ] AssemblyInfo.cs のフォーマット警告（14件）- IDE0055
- [ ] EF Core のロガー型キャストの改善（DataSeeder.cs:109）

### 優先度：低
- [ ] UI層のテストプロジェクト（DocumentFileManager.UI.Test）が未使用
- [ ] checklist.json のバリデーション機能がない

---

## 次回作業予定

### 即時対応（本日中）
1. ✅ TreeView表示の実装
2. チェックボックス機能の実装
3. UI全体のレイアウト調整

### 短期（今週中）
4. 資料登録UI実装
5. 紐づけUI実装
6. ファイル存在検証機能

### 中期（来週以降）
7. 画面キャプチャ機能
8. ユニットテスト作成
9. ユーザーマニュアル作成

---

## 完了した主要マイルストーン

- ✅ **2025-10-07 午前**: プロジェクト基盤完成（ドメイン層・インフラ層・DI・ロギング）
- ✅ **2025-10-07 午前**: データ基盤完成（シードデータ・JSON設定ファイル）
- ✅ **2025-10-07 午後**: UI設定外部化完成（PathSettings、UISettings、SettingsWindow）
- ✅ **2025-10-07 午後**: JSON-DB同期機能完成（checklist.jsonマスターデータ化）
- 🔄 **2025-10-07**: UI実装継続中（基本レイアウト完成、TreeView実装中）

---

## 参考資料

### 設計ドキュメント
- [要件定義.md](./docs/要件定義.md) - システム概要・機能要件
- [ドメインモデル定義書.md](./docs/ドメインモデル定義書.md) - エンティティ設計
- [データ設計書.md](./docs/データ設計書.md) - テーブル定義・ER図
- [ユースケース.md](./docs/ユースケース.md) - 利用シナリオ

### 技術仕様
- .NET 8 SDK
- Entity Framework Core 8.0
- Serilog 4.3.0
- WPF (Windows Presentation Foundation)

---

## 連絡先・課題管理

- GitHub Issues: （未設定）
- プロジェクト管理: このファイル（PROGRESS.md）で管理
