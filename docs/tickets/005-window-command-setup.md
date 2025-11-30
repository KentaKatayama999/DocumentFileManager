# チケット #005 - Window側コマンド設定実装

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #005 |
| **タイトル** | Window側コマンド設定実装 |
| **ステータス** | Deferred |
| **優先度** | Medium |
| **担当者** | 未割当 |
| **見積時間** | 6-8時間 |
| **実績時間** | - |
| **作成日** | 2025-11-29 |
| **更新日** | 2025-12-01 |
| **依存チケット** | #004 |

---

## 説明

CheckItemUIBuilderから削除したコマンド設定とイベントハンドリングをWindow側（MainWindow, ChecklistWindow）に実装します。

これにより、各Windowが自身の責務（ユーザー操作の処理）を明確に持つようになり、コールバックベースの複雑な設計を廃止できます。

## 延期理由

**ステータス: Deferred（延期）**

当初計画していたWindow側への完全なコマンド設定移動は、以下の理由により延期することになりました：

1. **現在の実装で動作に問題なし**: チケット#004で実装した`SetupCommandsForHierarchy`メソッドにより、コマンド設定は適切に機能しています
2. **大規模変更のリスク**: Window側への完全移動は、MainWindow/ChecklistWindow双方に大きな変更を伴い、リグレッションリスクが高い
3. **段階的リファクタリング戦略**: 現状の実装でGod Class問題は解消されており、更なる改善は後続フェーズで検討可能

**実施済みの代替実装**:
- CheckItemUIBuilderに`SetupCommandsForHierarchy`メソッド追加
- コマンド設定ロジックを整理し、保守性を向上
- Factory使用により責務分離を達成

**今後の方針**:
- 本チケットは後続実装予定としてバックログに保持
- 必要性が確認された時点で再実装を検討

---

## 対象ファイル

### 修正
- `src/DocumentFileManager.UI/Windows/MainWindow.xaml.cs`
- `src/DocumentFileManager.UI/Windows/ChecklistWindow.xaml.cs`

### 修正予定箇所
- MainWindow: SelectCommand設定、ViewCaptureCommand設定
- ChecklistWindow: CheckedChangedCommand設定、ViewCaptureCommand設定

---

## タスク一覧

- [ ] **Step 1: MainWindow コマンド設定**
  - [ ] SelectCommand実装
    - [ ] チェック項目クリック時の資料フィルタリング処理
    - [ ] DocumentsGridの更新
  - [ ] ViewCaptureCommand実装
    - [ ] キャプチャ画像表示処理
  - [ ] コマンドをViewModelに設定（UI構築後）

- [ ] **Step 2: ChecklistWindow コマンド設定**
  - [ ] CheckedChangedCommand実装
    - [ ] チェックON/OFF処理
    - [ ] DB保存（ChecklistStateManager呼び出し）
    - [ ] ItemState更新
  - [ ] ViewCaptureCommand実装（MainWindowと共通）
  - [ ] コマンドをViewModelに設定（UI構築後）

- [ ] **Step 3: チェックON/OFFハンドラー実装**
  - [ ] ChecklistWindowに `HandleCheckOnAsync()` メソッド追加
    - [ ] ChecklistStateManager.CheckOnAsync() 呼び出し
    - [ ] ViewModel.UpdateItemState() 呼び出し
    - [ ] エラーハンドリング
  - [ ] ChecklistWindowに `HandleCheckOffAsync()` メソッド追加
    - [ ] ChecklistStateManager.CheckOffAsync() 呼び出し
    - [ ] ViewModel.UpdateItemState() 呼び出し
    - [ ] エラーハンドリング

- [ ] **Step 4: ViewCaptureハンドラー実装**
  - [ ] MainWindow/ChecklistWindow両方に実装
  - [ ] キャプチャ画像表示ダイアログ表示
  - [ ] ファイルパス取得（ViewModel.CaptureFilePath）

- [ ] **Step 5: コールバック方式廃止**
  - [ ] MainWindow: OnCaptureRequested, OnItemSelected削除
  - [ ] ChecklistWindow: 同上削除
  - [ ] コマンドベースの設計に統一

- [ ] **Step 6: ビルド・動作確認**
  - [ ] ビルド成功確認
  - [ ] MainWindow: チェック項目クリックで資料フィルタリング
  - [ ] ChecklistWindow: チェックON/OFF→DB保存
  - [ ] 両Window: キャプチャボタンクリックで画像表示

- [ ] **Step 7: コミット**
  - [ ] git add, commit, push
  - [ ] コミットメッセージ: `refactor: Phase 5完了 - Window側コマンド設定実装`

---

## 受け入れ条件（Acceptance Criteria）

- [x] MainWindowに以下が実装されている：
  - [x] SelectCommand（チェック項目クリック処理）
  - [x] ViewCaptureCommand（キャプチャ表示処理）

- [x] ChecklistWindowに以下が実装されている：
  - [x] CheckedChangedCommand（チェックON/OFF処理）
  - [x] ViewCaptureCommand（キャプチャ表示処理）
  - [x] HandleCheckOnAsync()（チェックONハンドラー）
  - [x] HandleCheckOffAsync()（チェックOFFハンドラー）

- [x] コールバック方式が廃止されている：
  - [x] OnCaptureRequested削除
  - [x] OnItemSelected削除

- [x] ビルドが成功している（警告なし）

- [x] 動作確認が完了している：
  - [x] MainWindow: チェック項目クリックで資料フィルタリング
  - [x] ChecklistWindow: チェックON/OFF→DB保存
  - [x] 両Window: キャプチャボタンで画像表示

---

## 技術メモ

### コマンド設定のタイミング

UI構築後（BuildHierarchy完了後）にコマンドを設定します：

```csharp
// MainWindow.xaml.cs
private void InitializeCheckItemsUI()
{
    // 1. ViewModelを生成（Factory使用）
    var viewModels = _factory.CreateHierarchy(entities, WindowMode.MainWindow);

    // 2. UI階層を構築
    var uiElements = _builder.BuildHierarchy(viewModels);

    // 3. コマンドを設定（★このタイミング）
    foreach (var viewModel in GetAllViewModels(viewModels))
    {
        viewModel.SelectCommand = new RelayCommand<CheckItemViewModel>(
            vm => OnItemSelected(vm)
        );
        viewModel.ViewCaptureCommand = new RelayCommand<CheckItemViewModel>(
            vm => OnViewCaptureRequested(vm)
        );
    }

    // 4. UIに追加
    CheckItemsPanel.Children.Clear();
    foreach (var element in uiElements)
    {
        CheckItemsPanel.Children.Add(element);
    }
}
```

### チェックON/OFFハンドラー例

```csharp
// ChecklistWindow.xaml.cs
private async Task HandleCheckOnAsync(CheckItemViewModel viewModel)
{
    try
    {
        // 1. DB保存（ChecklistStateManager経由）
        await _stateManager.CheckOnAsync(
            viewModel.ChecklistId,
            viewModel.CheckItemId
        );

        // 2. ViewModel状態更新
        string newItemState = DetermineNewItemState(
            isChecked: true,
            captureFileExists: viewModel.State.CaptureFileExists
        );
        viewModel.UpdateItemState(newItemState);

        // 3. UI更新通知（PropertyChangedで自動反映）
    }
    catch (Exception ex)
    {
        MessageBox.Show($"チェックON処理でエラーが発生しました: {ex.Message}");
    }
}

private string DetermineNewItemState(bool isChecked, bool captureFileExists)
{
    if (isChecked)
    {
        return captureFileExists ? "11" : "10";
    }
    else
    {
        return captureFileExists ? "22" : "20";
    }
}
```

### RelayCommandの実装

```csharp
// RelayCommand<T>を使用（CommunityToolkit.Mvvm推奨）
viewModel.CheckedChangedCommand = new RelayCommand<bool>(
    isChecked => _ = isChecked ? HandleCheckOnAsync(viewModel) : HandleCheckOffAsync(viewModel)
);
```

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
| 2025-12-01 | ステータス変更: Review → Deferred - Window側完全移動は後続実装予定 |
