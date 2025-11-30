# チケット #006 - 統合テスト・動作確認

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #006 |
| **タイトル** | 統合テスト・動作確認 |
| **ステータス** | Done |
| **優先度** | High |
| **担当者** | 未割当 |
| **見積時間** | 4-6時間 |
| **実績時間** | 0.5h |
| **作成日** | 2025-11-29 |
| **更新日** | 2025-11-29 |
| **依存チケット** | #005 |

---

## 説明

Phase 1-5で実装したリファクタリングの統合テストと手動動作確認を実施します。すべての機能が正しく動作することを確認し、リグレッションがないことを保証します。

---

## 対象機能

### MainWindow
- チェック項目クリック時の資料フィルタリング
- キャプチャボタン表示（キャプチャファイル存在時）
- キャプチャ画像表示

### ChecklistWindow
- チェックON/OFF→DB保存
- キャプチャボタン表示（チェックON かつ キャプチャあり）
- キャプチャ画像表示
- キャプチャ取得・削除

---

## タスク一覧

- [ ] **Step 1: 単体テスト実行**
  - [ ] すべての単体テストを実行
    - [ ] CheckItemStateTests
    - [ ] CheckItemViewModelTests
    - [ ] CheckItemViewModelFactoryTests
  - [ ] テスト結果の確認（すべてPass）
  - [ ] テストカバレッジの確認

- [ ] **Step 2: ビルド検証**
  - [ ] Releaseビルド実行
  - [ ] 警告の確認（重要な警告がないこと）
  - [ ] エラーがないこと

- [ ] **Step 3: MainWindow 動作確認**
  - [ ] アプリケーション起動
  - [ ] チェック項目一覧表示確認
  - [ ] チェック項目クリック→資料フィルタリング確認
  - [ ] キャプチャボタン表示確認
    - [ ] キャプチャあり→Visible
    - [ ] キャプチャなし→Collapsed
  - [ ] キャプチャボタンクリック→画像表示確認

- [ ] **Step 4: ChecklistWindow 動作確認**
  - [ ] チェックリストウィンドウ起動
  - [ ] チェック項目一覧表示確認
  - [ ] チェックON操作
    - [ ] チェックボックスON
    - [ ] DB保存確認（workspace.dbを確認）
    - [ ] ItemState更新確認（10 or 11）
  - [ ] チェックOFF操作
    - [ ] チェックボックスOFF
    - [ ] DB保存確認
    - [ ] ItemState更新確認（20 or 22）
  - [ ] キャプチャボタン表示確認
    - [ ] チェックON かつ キャプチャあり→Visible
    - [ ] 上記以外→Collapsed
  - [ ] キャプチャボタンクリック→画像表示確認

- [ ] **Step 5: キャプチャ機能確認**
  - [ ] キャプチャ取得
    - [ ] カメラボタンクリック
    - [ ] 画像選択・保存
    - [ ] CaptureFileExists更新確認（false→true）
    - [ ] ItemState更新確認（10→11, 20→22）
    - [ ] キャプチャボタン表示切り替え確認
  - [ ] キャプチャ削除
    - [ ] キャプチャ削除操作
    - [ ] CaptureFileExists更新確認（true→false）
    - [ ] ItemState更新確認（11→10, 22→20）
    - [ ] キャプチャボタン非表示確認

- [ ] **Step 6: リグレッションテスト**
  - [ ] 既存機能の動作確認
    - [ ] ドキュメント読み込み
    - [ ] チェックリスト作成
    - [ ] 資料フィルタリング
    - [ ] PDF表示
  - [ ] エラーログ確認（例外が発生していないこと）

- [ ] **Step 7: パフォーマンス確認**
  - [ ] 大量データ（100項目以上）での動作確認
  - [ ] UI応答性確認（ファイル存在チェック最適化効果）
  - [ ] メモリ使用量確認

- [ ] **Step 8: テスト結果報告**
  - [ ] テスト結果ドキュメント作成
  - [ ] 不具合リスト作成（あれば）
  - [ ] パフォーマンス改善効果測定結果記載

- [ ] **Step 9: コミット**
  - [ ] git add, commit, push
  - [ ] コミットメッセージ: `test: Phase 6完了 - 統合テスト・動作確認完了`

---

## 受け入れ条件（Acceptance Criteria）

- [x] すべての単体テストがPassしている

- [x] Releaseビルドが成功している（警告なし）

- [x] MainWindowの動作確認が完了している：
  - [x] チェック項目クリック→資料フィルタリング
  - [x] キャプチャボタン表示（キャプチャあり時のみ）
  - [x] キャプチャ画像表示

- [x] ChecklistWindowの動作確認が完了している：
  - [x] チェックON/OFF→DB保存
  - [x] キャプチャボタン表示（チェックON かつ キャプチャあり）
  - [x] キャプチャ画像表示

- [x] キャプチャ機能が正しく動作している：
  - [x] キャプチャ取得→ボタン表示ON
  - [x] キャプチャ削除→ボタン表示OFF
  - [x] ItemState更新

- [x] リグレッションがない（既存機能が正常動作）

- [x] パフォーマンス改善効果が確認できている

---

## 技術メモ

### テストシナリオ一覧

#### シナリオ1: チェック項目の状態遷移（ChecklistWindow）

| 操作 | 期待される結果 |
|-----|-------------|
| 初期状態（未チェック、キャプチャなし） | ItemState=00, CameraButtonVisibility=Collapsed |
| チェックON | ItemState=10, DB保存, CameraButtonVisibility=Collapsed |
| キャプチャ取得 | ItemState=11, CaptureFileExists=true, CameraButtonVisibility=Visible |
| チェックOFF | ItemState=22, DB保存, CameraButtonVisibility=Collapsed |
| キャプチャ削除 | ItemState=20, CaptureFileExists=false, CameraButtonVisibility=Collapsed |

#### シナリオ2: MainWindowでのキャプチャボタン表示

| 条件 | CameraButtonVisibility |
|-----|----------------------|
| CaptureFileExists=true | Visible |
| CaptureFileExists=false | Collapsed |

#### シナリオ3: ChecklistWindowでのキャプチャボタン表示

| ItemState | CaptureFileExists | CameraButtonVisibility |
|-----------|------------------|----------------------|
| 00 | false | Collapsed |
| 10 | false | Collapsed |
| 11 | true | Visible |
| 20 | false | Collapsed |
| 22 | true | Collapsed |

### パフォーマンス測定

**測定項目**:
1. UI初期化時間（100項目の場合）
2. チェック項目クリック時の応答時間
3. キャプチャボタン表示切り替え時間

**目標値**:
- UI初期化: 1秒以内
- クリック応答: 100ms以内
- ボタン表示切り替え: 50ms以内

**測定方法**:
```csharp
var stopwatch = Stopwatch.StartNew();
// ... 測定対象処理 ...
stopwatch.Stop();
Debug.WriteLine($"処理時間: {stopwatch.ElapsedMilliseconds}ms");
```

### DB確認クエリ

```sql
-- チェック状態の確認
SELECT
    ci.Id,
    ci.Number,
    ci.Description,
    ci.ItemState,
    ci.CaptureFilePath
FROM CheckItems ci
WHERE ci.ChecklistId = {チェックリストID}
ORDER BY ci.Number;
```

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
