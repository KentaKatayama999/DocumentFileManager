# Component図: Document File Manager - UI Application

## 概要

UI Application コンテナ内の主要コンポーネントを示すComponent図（C4モデル レベル3）です。MVVMパターン、サービス層、インフラ層の構成を可視化します。

- **対象読者**: 開発者、実装担当者
- **目的**: UI Applicationの内部構造と依存関係を理解する

## 構成要素

### UI Layer (Presentation)

#### Component: MainWindow

- **種別**: WPF Window
- **技術**: XAML, Code-Behind
- **責務**:
  - メインウィンドウの表示
  - 資料一覧の表示（DataGrid）
  - フィルタリング機能
  - 資料の登録・閲覧操作のトリガー
- **判断根拠**:
  - Windows/MainWindow.xaml.cs
  - AppInitializer.cs L121で登録

#### Component: ChecklistWindow

- **種別**: WPF Window
- **技術**: XAML, Code-Behind
- **責務**:
  - チェックリスト専用ウィンドウの表示
  - チェック項目の一覧表示
  - 状態変更（チェックON/OFF）のトリガー
  - 画面キャプチャのトリガー
  - 最新リンク資料の紐づけ表示（青色強調）
- **判断根拠**:
  - Windows/ChecklistWindow.xaml.cs
  - HANDOFF.md L62-79のアーキテクチャ図

#### Component: CheckItemViewModel

- **種別**: ViewModel (CommunityToolkit.Mvvm)
- **技術**: ObservableObject, RelayCommand
- **責務**:
  - チェック項目の表示ロジック
  - CheckItemStateの保持・管理
  - IsLinkedToCurrentDocumentプロパティ（紐づけ表示フラグ）
  - ViewとModelの間のバインディング
- **判断根拠**:
  - ViewModels/CheckItemViewModel.cs
  - HANDOFF.md L66-68
  - チケット#002, #004実装

#### Component: CheckItemViewModelFactory

- **種別**: Factory
- **技術**: DI, Factory Pattern
- **責務**:
  - CheckItemViewModelの生成
  - 依存解決（documentRootPath, logger）
  - UI構築時の一元化
- **判断根拠**:
  - Factories/CheckItemViewModelFactory.cs
  - AppInitializer.cs L110-115
  - HANDOFF.md L71

#### Component: CheckItemUIBuilder

- **種別**: UI Helper
- **技術**: Builder Pattern
- **責務**:
  - UI構築ヘルパー
  - 最新リンク判定（IsLatestLinkAsync）
  - SetLinkedToCurrentDocumentFlag（紐づけ表示フラグ設定）
- **判断根拠**:
  - Helpers/CheckItemUIBuilder.cs
  - HANDOFF.md L74, L83-94
  - v1.3.2新機能実装

### Model Layer

#### Component: CheckItemState

- **種別**: Model
- **技術**: State Pattern
- **責務**:
  - チェック項目の状態管理
  - 状態パターン実装（11未チェック, 10チェック済等）
  - 状態遷移ロジックの委譲
- **判断根拠**:
  - Models/CheckItemState.cs
  - HANDOFF.md L67
  - チケット#001実装（TDD）

#### Component: CheckItemTransition

- **種別**: Model
- **技術**: Strategy Pattern
- **責務**:
  - 状態遷移ロジック
  - RestoreTo11WithCapture（復帰処理）
  - Capture処理
- **判断根拠**:
  - Models/CheckItemTransition.cs
  - HANDOFF.md L78, L48
  - v1.3.2機能実装

### Application Layer (Services)

#### Component: DocumentService

- **種別**: Application Service
- **技術**: Scoped Service (DI)
- **責務**:
  - 資料管理サービス
  - 単一ファイル登録（RegisterDocumentAsync）
  - 複数ファイル一括登録（RegisterDocumentsAsync）
- **判断根拠**:
  - Services/DocumentService.cs
  - IDocumentService インターフェース
  - README.md L220-238のサービスAPI

#### Component: ChecklistService

- **種別**: Application Service
- **技術**: Scoped Service (DI)
- **責務**:
  - チェックリスト管理サービス
  - 新規チェックリスト作成（CreateNewChecklistAsync）
  - チェックリスト存在確認（ChecklistExists）
- **判断根拠**:
  - Services/ChecklistService.cs
  - IChecklistService インターフェース
  - README.md L250-276のサービスAPI

#### Component: ScreenCaptureService

- **種別**: Application Service
- **技術**: Singleton Service (DI)
- **責務**:
  - 画面キャプチャ機能
  - スクリーンショット取得
  - 画像として保存（captures/）
- **判断根拠**:
  - Services/ScreenCaptureService.cs
  - キャプチャ機能（README.md L21）

#### Component: ChecklistStateManager

- **種別**: Application Service
- **技術**: Scoped Service (DI)
- **責務**:
  - チェックリスト状態管理
  - キャプチャ削除（DeleteCaptureFileAsync）
  - 全ドキュメントのキャプチャ情報クリア
  - LinkedAt更新
- **判断根拠**:
  - Services/ChecklistStateManager.cs
  - HANDOFF.md L77, L96-103
  - v1.3.2新機能実装

#### Component: DataIntegrityService

- **種別**: Application Service
- **技術**: Scoped Service (DI)
- **責務**:
  - データベースとファイルシステムの整合性チェック
  - 整合性レポート生成
- **判断根拠**:
  - Services/DataIntegrityService.cs
  - README.md L26（データ整合性チェック機能）

#### Component: WpfDialogService

- **種別**: Infrastructure Service
- **技術**: Singleton Service (DI)
- **責務**:
  - ダイアログサービス
  - 確認ダイアログ表示（ShowConfirmationDialog）
  - IDialogServiceインターフェース実装
- **判断根拠**:
  - Services/WpfDialogService.cs
  - Services/Abstractions/IDialogService.cs
  - チケット#001実装

### Initialization

#### Component: AppInitializer

- **種別**: Initializer (Static Class)
- **技術**: Microsoft.Extensions.Hosting, DI Container
- **責務**:
  - アプリケーション初期化
  - DIコンテナ構築（CreateHost）
  - サービス登録（Repository, Service, Factory等）
  - データベースマイグレーション（InitializeDatabaseAsync）
  - グローバル例外ハンドラ設定
- **判断根拠**:
  - AppInitializer.cs
  - README.md L83-112のDI設定

## 関連・依存関係

### UI Layer内の関係

#### Rel: ユーザー → MainWindow / ChecklistWindow

- **内容**: 操作
- **プロトコル/方法**: WPF（マウス、キーボード）
- **理由**: WPFアプリケーションのUI

#### Rel: ChecklistWindow → CheckItemViewModel

- **内容**: データバインディング
- **プロトコル/方法**: MVVM（INotifyPropertyChanged）
- **理由**: MVVMパターン実装

#### Rel: CheckItemViewModelFactory → CheckItemViewModel

- **内容**: 生成
- **プロトコル/方法**: Factory Pattern
- **理由**: ViewModelの一元的な生成

#### Rel: CheckItemUIBuilder → CheckItemViewModel

- **内容**: UI構築
- **プロトコル/方法**: Builder Pattern
- **理由**: UI構築ロジックの分離

### Model Layer内の関係

#### Rel: CheckItemViewModel → CheckItemState

- **内容**: 状態管理
- **プロトコル/方法**: Has-A（保持）
- **理由**: ViewModelが状態オブジェクトを保持

#### Rel: CheckItemState → CheckItemTransition

- **内容**: 状態遷移
- **プロトコル/方法**: Delegation（委譲）
- **理由**: 状態遷移ロジックの分離

### Service Layer との関係

#### Rel: MainWindow → DocumentService

- **内容**: 資料登録
- **プロトコル/方法**: DI（コンストラクタインジェクション）
- **理由**: 資料登録機能の呼び出し

#### Rel: MainWindow → ChecklistService

- **内容**: チェックリスト操作
- **プロトコル/方法**: DI（コンストラクタインジェクション）
- **理由**: チェックリスト管理機能の呼び出し

#### Rel: CheckItemViewModel → ChecklistStateManager

- **内容**: 状態更新
- **プロトコル/方法**: DI（コンストラクタインジェクション）
- **理由**: チェック状態変更、キャプチャ削除

#### Rel: CheckItemUIBuilder → ChecklistStateManager

- **内容**: 最新リンク判定
- **プロトコル/方法**: DI（コンストラクタインジェクション）
- **理由**: IsLatestLinkAsync呼び出し

#### Rel: ChecklistStateManager → WpfDialogService

- **内容**: 確認ダイアログ
- **プロトコル/方法**: DI（コンストラクタインジェクション）
- **理由**: 復帰確認ダイアログ表示

### Infrastructure Layer との関係

#### Rel: DocumentService → Infrastructure

- **内容**: データ永続化
- **プロトコル/方法**: Repository（IDocumentRepository）
- **理由**: 資料メタデータの保存

#### Rel: ChecklistService → Infrastructure

- **内容**: データ永続化
- **プロトコル/方法**: Repository（ICheckItemRepository）
- **理由**: チェック項目の永続化

#### Rel: DataIntegrityService → Infrastructure

- **内容**: 整合性チェック
- **プロトコル/方法**: Repository（複数）
- **理由**: DB/FS間の整合性検証

### External との関係

#### Rel: ChecklistStateManager → File System

- **内容**: キャプチャ削除
- **プロトコル/方法**: File I/O（File.Delete）
- **理由**: キャプチャファイルの物理削除

#### Rel: ScreenCaptureService → File System

- **内容**: 画像保存
- **プロトコル/方法**: File I/O（Bitmap.Save）
- **理由**: スクリーンショット画像の保存

#### Rel: Infrastructure → Database

- **内容**: 読み書き
- **プロトコル/方法**: EF Core
- **理由**: データ永続化

#### Rel: Infrastructure → Domain Core

- **内容**: エンティティ利用
- **プロトコル/方法**: 直接参照
- **理由**: Repository戻り値、EF Coreマッピング

### Initialization

#### Rel: AppInitializer → 各Service

- **内容**: 登録
- **プロトコル/方法**: DI Container（AddScoped, AddSingleton, AddTransient）
- **理由**: 依存性注入の設定

## 設計判断

### MVVMパターンの採用

- **決定**: UI層でMVVMパターンを採用
- **理由**:
  - WPFのデータバインディング機能を活用
  - UI層とロジック層の分離
  - テスタビリティ向上
  - CommunityToolkit.Mvvmによる実装支援
- **トレードオフ**:
  - 利点: UIとロジックの分離、テスト容易性
  - 欠点: 学習コスト、ボイラープレートコード増加

### CheckItemState の分離

- **決定**: 状態管理を専用クラス（CheckItemState）に分離
- **理由**:
  - ViewModelの肥大化防止
  - 状態遷移ロジックの一元化
  - テスタビリティ向上（状態のみの単体テスト可能）
  - State Patternによる拡張性
- **トレードオフ**:
  - 利点: 保守性、テスタビリティ、拡張性
  - 欠点: クラス数増加、間接参照

### Factoryパターンの導入

- **決定**: CheckItemViewModelFactoryでViewModelを生成
- **理由**:
  - ViewModel生成ロジックの一元化
  - 依存解決の集約
  - テスト時のモック挿入ポイント
- **トレードオフ**:
  - 利点: 生成ロジック集約、テスタビリティ
  - 欠点: 間接参照増加

### Builderパターンの導入

- **決定**: CheckItemUIBuilderでUI構築ロジックを分離
- **理由**:
  - UI構築ロジックの複雑化対応
  - 最新リンク判定ロジックの分離
  - ViewModelの責務軽減
- **トレードオフ**:
  - 利点: 責務分離、保守性向上
  - 欠点: クラス数増加

### Scopedサービスの活用

- **決定**: Service層の多くをScopedライフタイムで登録
- **理由**:
  - ウィンドウごとの独立したサービススコープ
  - DbContextのScopedライフタイムと一致
  - メモリリーク防止
- **トレードオフ**:
  - 利点: ライフサイクル管理容易、メモリ効率
  - 欠点: スコープ管理の複雑さ

### IDialogServiceインターフェース導入

- **決定**: ダイアログ表示をインターフェース化
- **理由**:
  - サービス層のWPF依存排除
  - テスト時のモック容易化
  - 将来的なダイアログ実装変更への対応
- **トレードオフ**:
  - 利点: テスタビリティ、抽象化
  - 欠点: インターフェース増加

### ChecklistStateManagerの責務拡大

- **決定**: v1.3.2でキャプチャ削除機能をChecklistStateManagerに追加
- **理由**:
  - 状態管理とキャプチャ管理の密接な関係
  - 既存のICheckItemDocumentRepository依存を活用
  - ダイアログサービス利用の一元化
- **トレードオフ**:
  - 利点: 関連機能の集約、既存DI活用
  - 欠点: クラスの責務増加（将来的な分離検討余地）

## 凡例

- **Component** (青): アプリケーション内のコンポーネント
- **Container_Ext** (グレー): 外部コンテナ
- **System_Ext** (グレー): 外部システム
- **Rel** (矢印): コンポーネント間の依存関係

## 関連ドキュメント

- **PlantUML図**: [component-ui.puml](./component-ui.puml)
- **上位レベル図**: [container.md](./container.md) - コンテナ図（レベル2）
- **実装ドキュメント**:
  - [../../../docs/tickets/001-checkitem-state-creation.md](../../tickets/001-checkitem-state-creation.md)
  - [../../../docs/tickets/002-checkitem-viewmodel-modification.md](../../tickets/002-checkitem-viewmodel-modification.md)
  - [../../../docs/tickets/003-checkitem-viewmodel-factory-creation.md](../../tickets/003-checkitem-viewmodel-factory-creation.md)
  - [../../../docs/tickets/004-checkitem-uibuilder-refactoring.md](../../tickets/004-checkitem-uibuilder-refactoring.md)
- **アーキテクチャドキュメント**: [../../../HANDOFF.md](../../../HANDOFF.md) L56-79
