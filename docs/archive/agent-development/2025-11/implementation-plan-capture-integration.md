# キャプチャ画像紐づけ機能 実装プラン

## 概要
チェック項目にチェックを入れた際に、該当箇所のキャプチャを取得し、資料とチェック項目に紐づけて保存する機能を実装する。

## ユーザーフロー
1. 資料をダブルクリック → ChecklistWindowとViewerWindowが開く
2. チェックすべき内容を資料内で見つける
3. チェックボックスをON → 「この箇所のキャプチャを取得しますか？」ダイアログ表示
4. 「はい」を選択 → キャプチャフロー開始（範囲選択→プレビュー→保存）
5. キャプチャ保存完了 → チェック項目の横に「🖼️」ボタンが動的に追加される
6. 🖼️ボタンをクリック → 保存済みキャプチャ画像を表示

## データベース構造（既存）
✅ 既に実装済み
- `CheckItemDocument.CaptureFile` フィールド（string, nullable）
- `CheckItemDocument.GetCaptureAbsolutePath(projectRoot)` メソッド
- `CheckItemDocument.CaptureExists(projectRoot)` メソッド

## 実装タスク

### Phase 1: 設定とディレクトリ構造
- [x] プロジェクトルートに `captures/` ディレクトリを作成
- [x] PathSettings に CapturesDirectory 設定を追加
  - ファイル: `src/DocumentFileManager.UI/Configuration/PathSettings.cs`
  - プロパティ: `public string CapturesDirectory { get; set; } = "captures";`

### Phase 2: ViewModelの拡張
- [x] CheckItemViewModel にキャプチャ関連プロパティを追加
  - ファイル: `src/DocumentFileManager.UI/ViewModels/CheckItemViewModel.cs`
  - プロパティ:
    - `public string? CaptureFilePath { get; set; }`
    - `public bool HasCapture => !string.IsNullOrEmpty(CaptureFilePath);`

### Phase 3: リポジトリの拡張
- [x] ICheckItemDocumentRepository に CaptureFile 更新メソッドを追加
  - ファイル: `src/DocumentFileManager.Infrastructure/Repositories/ICheckItemDocumentRepository.cs`
  - メソッド: `Task UpdateCaptureFileAsync(int checkItemDocumentId, string? captureFilePath);`
  - 注: nullable型（削除時にnullを設定可能）
- [x] CheckItemDocumentRepository に実装を追加
  - ファイル: `src/DocumentFileManager.Infrastructure/Repositories/CheckItemDocumentRepository.cs`

### Phase 4: キャプチャ画像ビューアの作成
- [x] CaptureImageViewerWindow.xaml を作成
  - ファイル: `src/DocumentFileManager.UI/CaptureImageViewerWindow.xaml`
  - 内容: 画像表示（ScrollViewer + Stretch="Uniform"）、削除ボタン
  - 注: 基本的なスクロール対応のみ（高度なズーム機能は将来的な改善点）
- [x] CaptureImageViewerWindow.xaml.cs を作成
  - ファイル: `src/DocumentFileManager.UI/CaptureImageViewerWindow.xaml.cs`
  - 機能:
    - コンストラクタで画像パスを受け取る
    - 画像を表示（BitmapImage + CacheOption.OnLoad）
    - 削除ボタンでファイルを物理削除
    - IsDeleted プロパティで削除状態を通知

### Phase 5: ImagePreviewWindow の拡張
- [x] ImagePreviewWindow に自動保存モードを追加
  - ファイル: `src/DocumentFileManager.UI/ImagePreviewWindow.xaml.cs`
  - 変更点:
    - コンストラクタに `string? autoSavePath = null` パラメータを追加
    - autoSavePathが指定されている場合、保存ボタンクリック時に自動的にそのパスに保存
    - 保存後に `public string? SavedFilePath { get; private set; }` プロパティに保存先を設定

### Phase 6: CheckItemUIBuilder の修正
- [x] CreateCheckBox メソッドを修正してStackPanelレイアウトに変更
  - ファイル: `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs`
  - 変更内容:
    ```csharp
    // 従来: CheckBox単体を返す
    // 新規: StackPanel { CheckBox, Button(🖼️) } を返す
    ```
  - レイアウト:
    ```
    StackPanel (Horizontal)
    ├── CheckBox "平面図"
    └── Button "📷" (初期状態: Visibility.Collapsed)
    ```
  - 画像確認ボタンの実装:
    - StackPanel の Tag に匿名型（CheckBox, ImageButton, ViewModel）を保持
    - 各要素（CheckBox, ImageButton）の Tag に ViewModel を保持
    - Click イベントで CaptureImageViewerWindow を開く
    - キャプチャが存在する場合のみ表示（Visibility.Visible）
    - ボタンデザイン:
      - 絵文字: 📷（カメラ）
      - サイズ: 24×20ピクセル
      - フォントサイズ: 11
      - 背景色: RGB(255, 220, 220) 薄い赤
      - 枠線色: RGB(200, 160, 160) 薄い赤茶
      - カーソル: Hand（クリック可能を示す）
      - 配置: CheckBox の右隣、5px のマージン

- [x] CheckBox.Checked イベントハンドラを修正
  - チェック後に「キャプチャを取得しますか？」ダイアログを表示
  - 「はい」の場合、ChecklistWindow の `PerformCaptureForCheckItem` を呼び出し

### Phase 7: ChecklistWindow の拡張
- [x] PerformCaptureForCheckItem メソッドを追加
  - ファイル: `src/DocumentFileManager.UI/ChecklistWindow.xaml.cs`
  - パラメータ: `CheckItemViewModel viewModel, UIElement checkBoxContainer`
  - 処理フロー:
    1. キャプチャファイルパスを生成: `captures/document_{DocumentId}/checkitem_{CheckItemId}_{timestamp}.png`
    2. ディレクトリが存在しない場合は作成
    3. ScreenCaptureOverlay で範囲選択
    4. ImagePreviewWindow を自動保存モードで開く
    5. 保存成功時:
       - CheckItemDocument の CaptureFile を更新
       - ViewModel の CaptureFilePath を更新
       - 📷ボタンを表示 (Visibility.Visible)

- [x] CheckItemUIBuilder に ChecklistWindow の参照を渡せるようにする
  - デリゲート `Func<CheckItemViewModel, UIElement, Task>? onCaptureRequested` を追加
  - ChecklistWindow から呼び出せるようにする

### Phase 8: 統合とテスト
- [x] プロジェクトをビルド
- [x] 動作確認:
  - [x] 資料を開く
  - [x] チェックボックスをON
  - [x] ダイアログで「はい」を選択
  - [x] キャプチャ範囲選択
  - [x] プレビュー確認
  - [x] 保存ボタンクリック
  - [x] 📷ボタンが表示されることを確認
  - [x] 📷ボタンをクリックして画像が表示されることを確認
  - [x] アプリを再起動して画像が復元されることを確認
- [x] エラーハンドリングの確認:
  - [x] ディレクトリ作成失敗時 - エラーメッセージ表示
  - [x] ファイル保存失敗時 - エラーメッセージ表示
  - [x] 画像が既に削除されている場合 - 削除機能で対応

### Phase 9: クリーンアップ
- [x] 不要なログを削除 - 確認済み、適切なログレベルで記録されている
- [x] コメントを追加 - 全てのメソッドに適切なXMLコメントが付いている
- [x] コードフォーマット確認 - ビルド成功、フォーマット問題なし

## ディレクトリ構造
```
プロジェクトルート/
├── captures/                          # キャプチャ画像保存先
│   ├── document_1/                    # 資料ID=1のキャプチャ
│   │   ├── checkitem_5_20251017_101530.png
│   │   └── checkitem_7_20251017_102045.png
│   └── document_2/
│       └── checkitem_3_20251017_103000.png
├── test-files/                        # テスト用資料
└── workspace.db                       # データベース
```

## データベース更新内容
既存の `CheckItemDocuments` テーブルの `CaptureFile` カラムにファイルパスを保存:
```sql
-- 例
UPDATE CheckItemDocuments
SET CaptureFile = 'captures/document_1/checkitem_5_20251017_101530.png'
WHERE Id = 123;
```

## UI変更イメージ

### Before（現在）
```
☐ 設計図
☑ 平面図
☐ 立面図
```

### After（実装後）
```
☐ 設計図
☑ 平面図 [📷]
☐ 立面図
```

注: 📷ボタンは薄い赤色背景（RGB: 255, 220, 220）で表示され、キャプチャが保存されている場合のみ表示されます。

## 注意事項
- キャプチャ画像は相対パスで保存（プロジェクトの可搬性を確保）
- 同一チェック項目に複数回キャプチャした場合、最新のもので上書き
- CheckItemDocument レコードが削除された場合、物理ファイルも削除する必要がある（将来的な改善点）

## 実装完了の定義
- [x] すべてのタスクが完了
- [x] ビルドエラーなし（0エラー、14警告）
- [x] 基本的な動作確認完了
- [x] エラーハンドリング確認完了

## 実装完了日
2025年10月17日

## 変更履歴

### 2025年10月17日 - プラン修正（実装コードとの整合性確保）
**修正箇所**:
1. **Phase 3**: UpdateCaptureFileAsync のパラメータを `string?` (nullable) に修正
2. **Phase 4**: 画像ビューアの機能説明を「拡大縮小」から「スクロール対応」に修正
3. **Phase 6**: ボタンデザインの詳細仕様を追加（サイズ、フォント、色、カーソル等）
4. **Phase 6**: StackPanel の Tag プロパティの説明を明確化
5. **UI変更イメージ**: 絵文字を「🖼️」から「📷」に統一

**理由**:
- 削除機能でDBにnullを設定する必要があるため、nullable型が適切
- ユーザーフィードバックで「📷」（カメラ）絵文字、薄い赤背景に決定
- 実装された詳細なボタン仕様をプランに反映
- 基本的なズーム機能は将来的な改善点として記録

詳細は `docs/implementation-discrepancy-report.md` を参照。
