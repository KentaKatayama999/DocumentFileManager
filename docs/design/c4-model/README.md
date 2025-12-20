# C4モデル - Document File Manager

このディレクトリには、Document File ManagerシステムのC4モデルアーキテクチャ図が格納されています。

## C4モデルとは

C4モデルは、ソフトウェアアーキテクチャを4つの抽象度レベルで可視化する手法です。

1. **Context（コンテキスト）** - システムと外部との関係
2. **Container（コンテナ）** - 実行可能ユニット、データストアの配置
3. **Component（コンポーネント）** - コンテナ内部の主要コンポーネント
4. **Code（コード）** - クラス図（本プロジェクトでは作成していません）

## ドキュメント構成

各レベルについて、以下の2種類のファイルを用意しています。

| レベル | PlantUML図 | 説明文書（Markdown） | 内容 |
|--------|-----------|---------------------|------|
| **Level 1: Context** | [context.puml](./context.puml) | [context.md](./context.md) | システム全体と外部との関係 |
| **Level 2: Container** | [container.puml](./container.puml) | [container.md](./container.md) | 主要コンテナと技術スタック |
| **Level 3: Component (UI)** | [component-ui.puml](./component-ui.puml) | [component-ui.md](./component-ui.md) | UI Application内部コンポーネント |

### PlantUML図の表示方法

PlantUML図（.pumlファイル）は以下の方法で表示できます：

#### Visual Studio Code

1. 拡張機能「PlantUML」をインストール
2. .pumlファイルを開く
3. `Alt + D` でプレビュー表示

#### オンライン

- [PlantUML Online Editor](http://www.plantuml.com/plantuml/uml/)
- .pumlファイルの内容をコピー&ペースト

#### コマンドライン

```bash
# PlantUMLのインストール（要Java）
# brew install plantuml  # macOS
# choco install plantuml # Windows

# PNG画像生成
plantuml context.puml
plantuml container.puml
plantuml component-ui.puml
```

## 各レベルの概要

### Level 1: System Context図

**対象読者**: ステークホルダー、プロジェクトマネージャー、アーキテクト

**内容**:
- システム（Document File Manager）
- ユーザー（エンドユーザー）
- 外部システム（外部アプリケーション、ファイルシステム）
- システム間の関係

**主な判断根拠**:
- README.mdの主な機能説明
- プロジェクト固有データ管理の設計
- 外部アプリケーション連携機能

### Level 2: Container図

**対象読者**: 開発者、アーキテクト

**内容**:
- UI Application（WPF, .NET 9.0）
- Document Viewer（WPF, .NET 9.0, WebView2）
- Domain Core（.NET 8.0）
- Infrastructure（.NET 8.0, EF Core）
- SQLite Database（workspace.db）
- File System（資料ファイル、キャプチャ画像等）

**主な判断根拠**:
- .csprojファイルの技術スタック
- プロジェクト参照関係
- Clean Architectureの採用

### Level 3: Component図（UI Application）

**対象読者**: 開発者、実装担当者

**内容**:
- **UI Layer**: MainWindow, ChecklistWindow, CheckItemViewModel
- **Factory/Builder**: CheckItemViewModelFactory, CheckItemUIBuilder
- **Model**: CheckItemState, CheckItemTransition
- **Service Layer**: DocumentService, ChecklistService, ScreenCaptureService, ChecklistStateManager, DataIntegrityService
- **Infrastructure Service**: WpfDialogService
- **Initializer**: AppInitializer

**主な判断根拠**:
- AppInitializer.csのDI登録
- HANDOFF.mdのアーキテクチャ図
- v1.3.2の実装内容（チケット#001-#006）

## アーキテクチャの特徴

### Clean Architecture

- **Domain Core**: ビジネスロジックの中核、外部依存なし
- **Infrastructure**: データアクセス、外部リソース連携
- **UI**: プレゼンテーション層、MVVM実装

依存方向: UI → Infrastructure → Domain Core

### MVVMパターン

- **View**: XAML（MainWindow.xaml, ChecklistWindow.xaml）
- **ViewModel**: CheckItemViewModel（CommunityToolkit.Mvvm）
- **Model**: CheckItemState, CheckItemTransition

### 依存性注入（DI）

- Microsoft.Extensions.Hosting, Microsoft.Extensions.DependencyInjection
- AppInitializer.csで一元管理
- Scoped, Singleton, Transientライフタイム管理

### 状態管理パターン

- **State Pattern**: CheckItemState
- **Strategy Pattern**: CheckItemTransition
- **Factory Pattern**: CheckItemViewModelFactory
- **Builder Pattern**: CheckItemUIBuilder

## 更新履歴

| 日付 | バージョン | 内容 |
|------|-----------|------|
| 2025-12-04 | v1.0 | 初版作成（Context, Container, Component-UI） |

## 関連ドキュメント

- [README.md](../../../README.md) - プロジェクト概要
- [HANDOFF.md](../../../HANDOFF.md) - セッションハンドオフドキュメント
- [FOLDER_STRUCTURE.md](../../../FOLDER_STRUCTURE.md) - フォルダ構成
- [要件定義.md](../../要件定義.md) - システム概要・機能要件
- [ユースケース.md](../../ユースケース.md) - 利用シナリオ
- [チケット一覧](../../tickets/index.md) - 実装チケット

## 次のステップ

### 追加検討中のC4図

1. **Component図（Infrastructure）**: Repositoryパターンの詳細
2. **Component図（Domain Core）**: エンティティ、値オブジェクトの関係
3. **Deployment図**: 配置構成（開発/本番環境）

### 更新が必要な場合

C4図の更新が必要な場合は、以下の手順で行ってください：

1. **PlantUMLファイルを更新** (.puml)
2. **Markdown説明文書を更新** (.md)
   - 構成要素の追加/削除
   - 判断根拠の記載
   - 設計判断の追加
3. **このREADMEを更新** (更新履歴に追記)

## 参考リンク

- [C4 Model公式サイト](https://c4model.com/)
- [C4-PlantUML](https://github.com/plantuml-stdlib/C4-PlantUML)
- [PlantUML公式](https://plantuml.com/)
