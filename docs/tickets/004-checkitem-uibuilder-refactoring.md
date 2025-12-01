# チケット #004 - CheckItemUIBuilder リファクタリング（Factory導入）

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #004 |
| **タイトル** | CheckItemUIBuilder リファクタリング（Factory導入） |
| **ステータス** | Done |
| **優先度** | Medium |
| **担当者** | 未割当 |
| **見積時間** | 6-8時間 |
| **実績時間** | 0.5h |
| **作成日** | 2025-11-29 |
| **更新日** | 2025-12-01 |
| **依存チケット** | #003 |

---

## 説明

CheckItemUIBuilderのGod Class問題を**段階的に**解消するため、ViewModel構築ロジックをFactoryに分離します。

当初は「200行以下への縮小」「コマンド設定のWindow側移動」を目標としていましたが、リグレッションリスクを考慮し、以下の方針に変更しました：

1. **実施**: ViewModel構築ロジックをFactoryに移譲
2. **維持**: コマンド設定とハンドラーは`CheckItemUIBuilder`内に残す（整理・集約）
3. **延期**: Window側への完全移動は後続フェーズで検討

---

## 対象ファイル

### 修正
- `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs`

### 実施した変更
- `BuildViewModelHierarchy()` → `_viewModelFactory.CreateHierarchy()` に置き換え
- `SetupCommandsForHierarchy()` メソッド追加（コマンド設定を整理・集約）
- DataTemplate使用による`CreateCheckBox()`の簡素化

### 維持しているメソッド（後続フェーズで移動検討）
- `SetupCommands()` - コマンド設定（MainWindow/ChecklistWindow分岐）
- `HandleCheckOnAsync()` - チェックONハンドラー
- `HandleCheckOffAsync()` - チェックOFFハンドラー
- `OnCaptureRequested`, `OnItemSelected` - コールバック

---

## タスク一覧

- [x] **Step 1: ViewModel構築ロジックをFactory移譲**
  - [x] `BuildViewModelHierarchy()` メソッド削除
  - [x] `_viewModelFactory.CreateHierarchy()` 呼び出しに置き換え
  - [x] ViewModelファクトリをDI注入

- [x] **Step 2: コマンド設定の整理・集約**
  - [x] `SetupCommandsForHierarchy()` メソッド追加
  - [x] 階層構造を再帰的に走査してコマンド設定
  - [x] MainWindow/ChecklistWindow分岐処理を維持

- [x] **Step 3: UI生成メソッドのクリーンアップ**
  - [x] `CreateCheckBox()` をDataTemplate使用に変更
  - [x] ContentControl + DataTemplate によるMVVM準拠

- [x] **Step 4: ビルド・テスト実行**
  - [x] ビルド成功確認
  - [x] 既存テストの更新
  - [x] テストPass確認

- [x] **Step 5: コミット**
  - [x] git add, commit, push

---

## 受け入れ条件（Acceptance Criteria）

- [x] ViewModel構築ロジックがFactoryに移譲されている
  - [x] `_viewModelFactory.CreateHierarchy()` を使用

- [x] コマンド設定が整理・集約されている
  - [x] `SetupCommandsForHierarchy()` で一元管理

- [x] DataTemplateを使用したUI生成に移行している
  - [x] `CreateCheckBox()` がContentControl + DataTemplateを使用

- [x] ビルドが成功している（警告なし）

- [x] 既存テストがすべてPassしている

---

## 実装結果

### 現在のCheckItemUIBuilder構成（約440行）

```
CheckItemUIBuilder
├── BuildAsync() - UI構築エントリポイント
├── SetupCommandsForHierarchy() - コマンド設定（階層走査）
├── SetupCommands() - 個別コマンド設定
├── HandleCheckOnAsync() - チェックONハンドラー
├── HandleCheckOffAsync() - チェックOFFハンドラー
├── CreateGroupBox() - GroupBox UI生成
├── CreateCheckBox() - ContentControl + DataTemplate
├── GetBorderBrush() - 枠線色取得
└── ResolveCaptureFilePath() - パス解決
```

### 達成した改善

| 項目 | 変更前 | 変更後 |
|-----|-------|-------|
| ViewModel構築 | Builder内で実装 | Factory経由 |
| UI生成方式 | コードビハインド | DataTemplate |
| コマンド設定 | 分散 | `SetupCommandsForHierarchy()`で集約 |
| テスタビリティ | 低 | Factory分離により向上 |

### 後続フェーズで検討する項目

- コマンド設定のWindow側移動
- ハンドラーメソッドのWindow側移動
- コールバック方式の廃止
- 200行以下への縮小

---

## 技術メモ

### Factory導入による責務分離

| 責務 | 変更前 | 変更後 |
|-----|-------|-------|
| Entity → ViewModel変換 | CheckItemUIBuilder | **CheckItemViewModelFactory** |
| UI要素生成 | CheckItemUIBuilder | CheckItemUIBuilder |
| コマンド設定 | CheckItemUIBuilder（分散） | CheckItemUIBuilder（集約） |
| イベントハンドリング | CheckItemUIBuilder | CheckItemUIBuilder |

### DataTemplate使用への移行

```csharp
// 変更前: コードビハインドでUI構築
private UIElement CreateCheckBox(CheckItemViewModel viewModel, int depth)
{
    var checkBox = new CheckBox { ... };
    var button = new Button { ... };
    // ... 複雑なUI構築コード
}

// 変更後: DataTemplate使用
private UIElement CreateCheckBox(CheckItemViewModel viewModel, int depth)
{
    var contentControl = new ContentControl
    {
        Content = viewModel,
        ContentTemplate = (DataTemplate)_containerElement.FindResource("CheckItemTemplate")
    };
    return contentControl;
}
```

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
| 2025-12-01 | 実装完了 - Factory導入、DataTemplate移行、コマンド設定集約 |
| 2025-12-01 | ドキュメント修正 - 実態に合わせてタイトル・内容を更新 |
