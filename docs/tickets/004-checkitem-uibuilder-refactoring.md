# チケット #004 - CheckItemUIBuilder縮小リファクタリング

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #004 |
| **タイトル** | CheckItemUIBuilder縮小リファクタリング |
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

CheckItemUIBuilderのGod Class問題を解消するため、ViewModel構築ロジックとコマンド設定を削除し、UI要素生成のみに責務を限定します。

約450行のクラスを200行以下に縮小し、保守性を大幅に向上させます。

---

## 対象ファイル

### 修正
- `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs`

### 削除予定メソッド
- `BuildViewModelHierarchy()`（Factory呼び出しに置き換え）
- `SetupCommands()`（Window側に移動）
- `HandleCheckOnAsync()`（Window側に移動）
- `HandleCheckOffAsync()`（Window側に移動）

### 維持するメソッド
- `BuildHierarchy()`（UI要素階層構築）
- `CreateGroupBox()`（GroupBox UI生成）
- `CreateCheckBox()`（CheckBox UI生成）

---

## タスク一覧

- [x] **Step 1: ViewModel構築ロジック削除**
  - [x] `BuildViewModelHierarchy()` メソッド削除
  - [x] Factory呼び出しに置き換え（`_factory.CreateHierarchy()`）
  - [x] ViewModelファクトリをDI注入

- [x] **Step 2: コマンド設定削除**
  - [x] `SetupCommands()` メソッド削除
  - [x] MainWindow/ChecklistWindow分岐処理削除
  - [x] コマンドバインディングをXAML/Window側に移動

- [x] **Step 3: ハンドラーメソッド削除**
  - [x] `HandleCheckOnAsync()` メソッド削除
  - [x] `HandleCheckOffAsync()` メソッド削除
  - [x] これらのロジックはWindow側で実装

- [x] **Step 4: コールバック方式廃止**
  - [x] `OnCaptureRequested` イベント削除
  - [x] `OnItemSelected` イベント削除
  - [x] コールバックベースの設計を廃止

- [x] **Step 5: UI生成メソッドのクリーンアップ**
  - [x] `CreateGroupBox()` メソッド整理
  - [x] `CreateCheckBox()` メソッド整理
  - [x] 不要なパラメータ削除

- [x] **Step 6: ビルド・テスト実行**
  - [x] ビルド成功確認
  - [x] 既存テストの更新（CheckItemUIBuilderテスト）
  - [x] テストPass確認

- [x] **Step 7: コミット**
  - [x] git add, commit, push
  - [x] コミットメッセージ: `refactor: Phase 4完了 - CheckItemUIBuilder縮小（God Class解消）`

---

## 受け入れ条件（Acceptance Criteria）

- [x] CheckItemUIBuilderが以下のみに責務を限定している：
  - [x] UI要素階層構築（ViewModel → UI要素）
  - [x] GroupBox/CheckBox UI生成

- [x] 以下が削除されている：
  - [x] ViewModel構築ロジック（Factoryに移譲）
  - [x] コマンド設定（Window側に移動）
  - [x] ハンドラーメソッド（Window側に移動）
  - [x] コールバックイベント（廃止）

- [x] ファイルサイズが200行以下に縮小している

- [x] ビルドが成功している（警告なし）

- [x] 既存テストがすべてPassしている

---

## 技術メモ

### God Class問題の解消

**変更前（約450行、5つの責務）**:
```
CheckItemUIBuilder
├── ViewModel構築（BuildViewModelHierarchy）
├── UI要素生成（CreateGroupBox, CreateCheckBox）
├── コマンド設定（SetupCommands）
├── イベントハンドリング（HandleCheckOn/OffAsync）
└── コールバック管理（OnCaptureRequested, OnItemSelected）
```

**変更後（約150行、1つの責務）**:
```
CheckItemUIBuilder
└── UI要素生成のみ（CreateGroupBox, CreateCheckBox）
```

### Single Responsibility Principle（SRP）の適用

| 責務 | 変更前 | 変更後 |
|-----|-------|-------|
| Entity → ViewModel変換 | CheckItemUIBuilder | CheckItemViewModelFactory |
| UI要素生成 | CheckItemUIBuilder | CheckItemUIBuilder |
| コマンド設定 | CheckItemUIBuilder | Window（MainWindow/ChecklistWindow） |
| イベントハンドリング | CheckItemUIBuilder | Window（MainWindow/ChecklistWindow） |

### 削除するコード例

```csharp
// ★削除対象: ViewModel構築ロジック
private ObservableCollection<CheckItemViewModel> BuildViewModelHierarchy(...)
{
    // ... 約100行のViewModel構築ロジック ...
    // → CheckItemViewModelFactory.CreateHierarchy() に置き換え
}

// ★削除対象: コマンド設定
private void SetupCommands(CheckItemViewModel viewModel, WindowMode mode)
{
    if (mode == WindowMode.MainWindow)
    {
        viewModel.SelectCommand = ...;
    }
    else
    {
        viewModel.CheckedChangedCommand = ...;
    }
    // → Window側で設定
}

// ★削除対象: ハンドラー
private async Task HandleCheckOnAsync(CheckItemViewModel viewModel)
{
    // ... チェックON処理 ...
    // → Window側で実装
}
```

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
| 2025-12-01 | 実装完了 - CheckItemUIBuilder縮小、Factory使用に変更、SetupCommandsForHierarchy追加 |
