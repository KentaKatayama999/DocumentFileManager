# システムコンテキスト図の説明

## システム

### Document File Manager
- **説明**: 技術資料とチェックリストを統合管理し、進捗を可視化するWPFアプリケーション。
- **技術**: WPF Desktop Application
- **責務**:
  - ユーザーに資料管理とチェックリスト機能を提供します。
  - 外部アプリケーションと連携してファイルを開いたり、画面キャプチャを取得したりします。

## ユーザー

### End User (資料管理者、チェック作業者)
- **役割**: Document Manager, Checker
- **責務**:
  - 資料の登録、チェックリストの操作、進捗確認を行います。
  - 外部アプリケーションを使用して資料の閲覧・編集を行います。

## 外部システム

### External Applications
- **例**: Office (Word, Excel), CAD, PDF Viewer, Mail など
- **役割**:
  - 実際の資料ファイルの閲覧や編集に使用されます。
  - Document File Manager から起動されたり、画面キャプチャの対象となったりします。

## 関連

- **End User** は **Document File Manager** を使用して、資料登録、チェックリスト操作、進捗確認を行います。
- **End User** は **External Applications** を直接操作して、資料の閲覧・編集を行います。
- **Document File Manager** は **External Applications** を呼び出してファイルを開きます (Process.Start)。
- **Document File Manager** は **External Applications** の画面キャプチャを取得します (Window API)。
