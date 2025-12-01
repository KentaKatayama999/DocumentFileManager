# コンポーネント図の説明

## Document File Manager コンテナ内のコンポーネント

### UI Layer (Presentation)
ユーザーとの対話を担当する層です。

1.  **MainWindow**
    *   アプリケーションのメインウィンドウ。
    *   資料一覧の表示、フィルタリング、資料の登録・閲覧操作を提供します。
2.  **ChecklistWindow**
    *   チェックリスト操作用ウィンドウ。
    *   チェック項目の表示、状態変更、画面キャプチャのトリガーを提供します。
3.  **CheckItemViewModel**
    *   チェック項目の表示ロジックと状態を管理します。
    *   View (Window) と Model (Entity) の間のバインディングを行います。

### Application Layer (Services)
ビジネスロジックとアプリケーションの調整を担当する層です。

4.  **ScreenCaptureService**
    *   画面キャプチャ機能を提供します。
    *   指定された範囲のスクリーンショットを取得し、画像として保存します。
5.  **ChecklistService**
    *   チェックリストに関するビジネスロジックを処理します。
6.  **DocumentService**
    *   ドキュメント管理に関するビジネスロジックを処理します。
7.  **DataIntegrityService**
    *   データベースとファイルシステムの整合性をチェックします。

### Infrastructure Layer (Data Access)
データの永続化と外部リソースへのアクセスを担当する層です。

8.  **DocumentRepository**
    *   `Documents` テーブルへのアクセスを提供します。
    *   資料の追加、検索、削除を行います。
9.  **CheckItemRepository**
    *   `CheckItems` テーブルへのアクセスを提供します。
    *   チェック項目の階層構造の取得や状態更新を行います。
10. **CheckItemDocumentRepository**
    *   `CheckItemDocuments` テーブル（中間テーブル）へのアクセスを提供します。
    *   チェック項目と資料の関連付けを管理します。
11. **ChecklistSaver**
    *   チェックリストの定義をJSONファイルとして保存・読み込みします。
12. **DocumentFileManagerContext**
    *   Entity Framework Core の DbContext。
    *   SQLiteデータベースへのセッションを管理します。

## 外部依存
*   **SQLite Database**: データストア (`workspace.db`)
*   **File System**: ファイルストア (`captures/`, `checklist.json` 等)
