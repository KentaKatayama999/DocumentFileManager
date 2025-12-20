# Container図: Document File Manager

## 概要

Document File Managerシステムの主要コンテナ（実行可能ユニット、データストア）を示すContainer図（C4モデル レベル2）です。アプリケーションの技術スタックと責務分担を可視化します。

- **対象読者**: 開発者、アーキテクト
- **目的**: システムの技術構成とコンテナ間の依存関係を理解する

## 構成要素

### Container: UI Application

- **種別**: WPFアプリケーション（実行可能ファイル）
- **技術スタック**:
  - .NET 9.0
  - WPF (Windows Presentation Foundation)
  - CommunityToolkit.Mvvm (MVVMパターン実装支援)
  - Serilog (ロギング)
- **責務**:
  - メインユーザーインターフェースの提供
  - 資料管理（登録、一覧、検索、フィルタリング）
  - チェックリスト操作（状態変更、紐づけ管理）
  - 画面キャプチャ機能
  - アプリケーションロジックの調整
- **判断根拠**:
  - DocumentFileManager.UI.csproj (OutputType: WinExe, UseWPF: true)
  - AppInitializer.csでのDI設定
  - MainWindow.xaml, ChecklistWindow.xaml等のWPFウィンドウ

### Container: Document Viewer

- **種別**: WPFアプリケーション（実行可能ファイル）
- **技術スタック**:
  - .NET 9.0
  - WPF
  - WebView2 (PDF表示用)
- **責務**:
  - ドキュメント（PDF、画像、テキスト）を別プロセスで表示
  - 内部ビューア（画像、テキスト、PDF）
  - 外部アプリ起動時のウィンドウ配置制御
  - 重いドキュメント表示によるUIブロック防止
- **判断根拠**:
  - DocumentFileManager.Viewer.csproj (OutputType: WinExe)
  - README.md L149-171のViewer説明
  - WebView2パッケージ参照（PDF表示）

### Container: Domain Core

- **種別**: クラスライブラリ (.dll)
- **技術スタック**:
  - .NET 8.0
  - Pure C# (外部依存なし)
- **責務**:
  - ドメインエンティティの定義（Document, CheckItem, CheckItemDocument）
  - 値オブジェクト（ItemStatus）
  - ビジネスルールのカプセル化
  - リポジトリインターフェースの定義（ICheckItemRepository等）
- **判断根拠**:
  - DocumentFileManager.csproj (TargetFramework: net8.0, IsPackable: true)
  - Entities/Document.cs, Entities/CheckItem.cs
  - Clean Architectureのドメイン層

### Container: Infrastructure

- **種別**: クラスライブラリ (.dll)
- **技術スタック**:
  - .NET 8.0
  - Entity Framework Core 8.0
  - SQLite Provider
- **責務**:
  - データアクセスロジックの実装
  - Repositoryパターン実装（CheckItemRepository, DocumentRepository等）
  - DbContext (DocumentManagerContext)
  - データベースマイグレーション
  - チェックリストファイル（JSON）の読み書き
- **判断根拠**:
  - DocumentFileManager.Infrastructure.csproj
  - Microsoft.EntityFrameworkCore.Sqlite パッケージ参照
  - Repositories/CheckItemRepository.cs等

### Container: SQLite Database

- **種別**: データベース
- **技術スタック**: SQLite
- **ファイル**: workspace.db
- **責務**:
  - ドキュメントメタデータの永続化（Documents テーブル）
  - チェック項目の永続化（CheckItems テーブル）
  - チェック項目と資料の関連情報（CheckItemDocuments テーブル）
  - LinkedAtタイムスタンプ管理（最新リンク判定用）
- **判断根拠**:
  - AppInitializer.cs L70-80のDbContext設定
  - README.md L173-189のデータベース構造説明
  - Migrations/

### Container: File System

- **種別**: ファイルシステム
- **技術スタック**: NTFS / FAT32
- **責務**:
  - 実際の資料ファイルの保存（PDF, DOCX, CAD等）
  - キャプチャ画像の保存（captures/）
  - チェックリスト定義の保存（checklist.json）
  - アプリケーション設定の保存（appsettings.json, appsettings.local.json）
  - ログファイルの保存（Logs/）
- **判断根拠**:
  - README.md L36-48のプロジェクトフォルダ構成
  - PathSettings.cs (CapturesDirectory, ChecklistFile等)

## 関連・依存関係

### Rel: エンドユーザー → UI Application

- **内容**: 操作
- **プロトコル/方法**: WPF UI
- **理由**: WPFデスクトップアプリケーションとして実装

### Rel: UI Application → Document Viewer

- **内容**: ファイル表示依頼
- **プロトコル/方法**: プロセス間通信
- **理由**:
  - 別プロセスで起動（重いドキュメント表示のUIブロック防止）
  - ViewerWindow起動（Process.Start相当）

### Rel: UI Application → Infrastructure

- **内容**: データ永続化
- **プロトコル/方法**: DI / Interface
- **理由**:
  - AppInitializer.csでRepositoryインターフェースを注入
  - ICheckItemRepository, IDocumentRepository等

### Rel: UI Application → Domain Core

- **内容**: ドメインロジック利用
- **プロトコル/方法**: 直接参照
- **理由**:
  - DocumentFileManager.UI.csproj → DocumentFileManager.csproj参照
  - エンティティ（Document, CheckItem）の利用

### Rel: Document Viewer → Domain Core

- **内容**: エンティティ参照
- **プロトコル/方法**: 直接参照
- **理由**:
  - DocumentFileManager.Viewer.csproj → DocumentFileManager.csproj参照

### Rel: Infrastructure → Domain Core

- **内容**: ドメインエンティティ実装
- **プロトコル/方法**: 直接参照
- **理由**:
  - DocumentFileManager.Infrastructure.csproj → DocumentFileManager.csproj参照
  - Repositoryがエンティティを返却

### Rel: Infrastructure → SQLite Database

- **内容**: 読み書き
- **プロトコル/方法**: EF Core / SQLite
- **理由**:
  - DocumentManagerContext.csでDbContextを実装
  - UseSqlite()でSQLite接続

### Rel: Infrastructure → File System

- **内容**: チェックリスト読み書き
- **プロトコル/方法**: File I/O
- **理由**:
  - ChecklistLoader.cs, ChecklistSaver.cs
  - checklist.jsonの読み書き

### Rel: UI Application → File System

- **内容**: 資料ファイル、キャプチャ画像保存
- **プロトコル/方法**: File I/O
- **理由**:
  - ScreenCaptureService.cs（画像保存）
  - DocumentService.cs（ファイルコピー）
  - ChecklistStateManager.cs（キャプチャ削除）

### Rel: Document Viewer → File System

- **内容**: ファイル読み込み
- **プロトコル/方法**: File I/O
- **理由**:
  - ImageViewer.cs, TextViewer.cs, PdfViewer.cs
  - ファイルパスからドキュメント読み込み

### Rel: UI Application → 外部アプリケーション

- **内容**: ファイル起動、画面キャプチャ
- **プロトコル/方法**: Process.Start, Win32 API
- **理由**:
  - 外部アプリケーション連携（Office, CAD等）
  - ScreenCaptureService.cs（Win32 API利用）

## 設計判断

### UI Application と Document Viewer の分離

- **決定**: Document Viewerを別プロセスとして実装
- **理由**:
  - 重いドキュメント（大容量PDF等）の表示によるメインUIのブロック防止
  - プロセス隔離によるクラッシュ時の影響範囲限定
  - WebView2の初期化コストをメインプロセスと分離
- **トレードオフ**:
  - 利点: UIレスポンス向上、安定性向上
  - 欠点: プロセス間通信のオーバーヘッド、デバッグ複雑化

### Domain Core を .NET 8.0 に固定

- **決定**: Domain CoreとInfrastructureは.NET 8.0、UI/Viewerは.NET 9.0
- **理由**:
  - Domain CoreはNuGet Package化対応（PackageId: DocumentFileManager）
  - 安定したLTS版（.NET 8.0）で配布
  - EF Core 8.0との互換性確保
- **トレードオフ**:
  - 利点: パッケージの安定性、広範な互換性
  - 欠点: .NET 9.0の新機能は利用不可

### SQLiteの採用

- **決定**: SQLiteをデータベースとして採用
- **理由**:
  - ファイルベースDB（workspace.db）→プロジェクト可搬性
  - サーバー不要（ローカルアプリケーション）
  - EF Core Sqliteプロバイダーの成熟
- **トレードオフ**:
  - 利点: セットアップ不要、軽量、可搬性
  - 欠点: 複数ユーザー同時アクセス不可、スケール限界

### プロジェクト固有のFile System配置

- **決定**: すべてのファイルをプロジェクトフォルダ（documentRootPath）内に配置
- **理由**:
  - プロジェクトの完全な可搬性
  - バージョン管理（Git等）で一元管理
  - 複数プロジェクトの独立管理
- **トレードオフ**:
  - 利点: データの一元管理、バックアップ容易
  - 欠点: フォルダサイズ増大、ネットワークドライブでの性能問題

### Clean Architectureの採用

- **決定**: Domain Core → Infrastructure → UI の依存方向
- **理由**:
  - ドメインロジックの独立性確保
  - テスタビリティ向上（Repositoryモック可能）
  - ビジネスルールの再利用性
- **トレードオフ**:
  - 利点: 保守性、テスタビリティ、再利用性
  - 欠点: 初期実装コスト増加、抽象化レイヤー増加

## 凡例

- **Container** (青): アプリケーション、クラスライブラリ
- **ContainerDb** (青): データベース
- **System_Ext** (グレー): 外部システム
- **Rel** (矢印): コンテナ間の依存関係

## 関連ドキュメント

- **PlantUML図**: [container.puml](./container.puml)
- **上位レベル図**: [context.md](./context.md) - システムコンテキスト図（レベル1）
- **下位レベル図**: [component-ui.md](./component-ui.md) - UI Applicationコンポーネント図（レベル3）
- **プロジェクト構成**: [../../../FOLDER_STRUCTURE.md](../../../FOLDER_STRUCTURE.md)
