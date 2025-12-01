# コンテナ図の説明

## コンテナ

### 1. Document File Manager (WPFアプリケーション)
- **技術スタック**: .NET 9.0, WPF, CommunityToolkit.Mvvm
- **責務**: 
  - ドキュメントとチェックリストを管理するためのメインユーザーインターフェースを提供します。
  - ユーザー操作、画面キャプチャ、アプリケーションロジックを処理します。
  - ユーザーとインフラストラクチャ層の間のデータフローを制御します。

### 2. Document Viewer (WPFアプリケーション)
- **技術スタック**: .NET 9.0, WPF, WebView2
- **責務**: 
  - ドキュメント（PDF、画像など）を別プロセスで表示します。
  - 重いドキュメントの表示がメインアプリケーションのUIをブロックしないようにします。

### 3. Domain Core (クラスライブラリ)
- **技術スタック**: .NET 8.0
- **責務**: 
  - ドメインエンティティ（Document, CheckItemなど）を含みます。
  - リポジトリやサービスのインターフェースを定義します。
  - 外部依存を持たない純粋なC#ロジックです。

### 4. Infrastructure (クラスライブラリ)
- **技術スタック**: .NET 8.0, Entity Framework Core (SQLite)
- **責務**: 
  - データアクセスロジックを実装します。
  - データベース接続とマイグレーションを管理します。
  - チェックリストの永続化のためのファイルシステム操作を処理します。

### 5. SQLite Database
- **技術スタック**: SQLite
- **責務**: 
  - ドキュメントとチェック項目のメタデータを保存します。
  - ドキュメントとチェック項目の関係を永続化します。
  - ファイル: `workspace.db`

### 6. File System
- **技術スタック**: NTFS/FAT32
- **責務**: 
  - 実際のドキュメントファイル（PDF, Excelなど）を保存します。
  - キャプチャ画像（`captures/` フォルダ）を保存します。
  - チェックリスト定義（`checklist.json`）を保存します。

## 関連
- **User** は **Document File Manager** を操作します。
- **Document File Manager** はデータ永続化のために **Infrastructure** を使用します。
- **Document File Manager** はファイルを表示するために **Document Viewer** を起動します。
- **Document File Manager** と **Infrastructure** は **Domain Core** に依存します。
- **Infrastructure** は **SQLite Database** を読み書きします。
- **Infrastructure** と **Document File Manager** は **File System** を読み書きします。
- **Document Viewer** は **File System** から読み込みます。
