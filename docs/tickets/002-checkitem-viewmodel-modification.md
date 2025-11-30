# チケット #002 - CheckItemViewModel修正

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #002 |
| **タイトル** | CheckItemViewModel修正 |
| **ステータス** | Done |
| **優先度** | High |
| **担当者** | 未割当 |
| **見積時間** | 4-6時間 |
| **実績時間** | 0.5h |
| **作成日** | 2025-11-29 |
| **更新日** | 2025-12-01 |
| **依存チケット** | #001 |

---

## 説明

CheckItemViewModelに`CheckItemState`プロパティを追加し、派生プロパティ（CameraButtonVisibility, IsCheckBoxEnabled）の計算ロジックをStateに委譲します。

また、パフォーマンス改善のため、`File.Exists()`をgetter内で毎回実行するのではなく、コンストラクタで1回だけ実行し、その結果をCheckItemStateに保持します。

---

## 対象ファイル

### 修正
- `src/DocumentFileManager.UI/ViewModels/CheckItemViewModel.cs`

### テスト更新
- `tests/DocumentFileManager.Tests/ViewModels/CheckItemViewModelTests.cs`（既存）

---

## タスク一覧

- [x] **Step 1: CheckItemStateプロパティ追加**
  - [x] `public CheckItemState State { get; private set; }` プロパティ追加
  - [x] コンストラクタで初期化

- [x] **Step 2: ファイル存在チェック最適化**
  - [x] コンストラクタで `File.Exists(CaptureFilePath)` を1回実行
  - [x] 結果を `CheckItemState.CaptureFileExists` に設定
  - [x] getter内の `File.Exists()` 呼び出しを削除

- [x] **Step 3: 派生プロパティの委譲**
  - [x] `CameraButtonVisibility` getter を `State.CameraButtonVisibility` に委譲
  - [x] `IsCheckBoxEnabled` getter を `State.IsCheckBoxEnabled` に委譲
  - [x] 既存プロパティとの互換性維持（INotifyPropertyChangedは維持）

- [x] **Step 4: 状態更新メソッド追加**
  - [x] `UpdateItemState(string newItemState)` メソッド追加
  - [x] `UpdateCaptureFileExists(bool exists)` メソッド追加
  - [x] PropertyChanged通知を適切に発火

- [x] **Step 5: テスト更新**
  - [x] 既存テストを更新（CheckItemState導入に対応）
  - [x] 新規テストケース追加
    - [x] State初期化テスト
    - [x] 派生プロパティ委譲テスト
    - [x] 状態更新メソッドテスト

- [x] **Step 6: ビルド・テスト実行**
  - [x] ビルド成功確認
  - [x] すべてのテストがPass確認

- [x] **Step 7: コミット**
  - [x] git add, commit, push
  - [x] コミットメッセージ: `refactor: Phase 2完了 - CheckItemViewModel修正（State導入）`

---

## 受け入れ条件（Acceptance Criteria）

- [x] `CheckItemViewModel`に`State`プロパティが追加されている

- [x] コンストラクタで`File.Exists()`を1回実行し、結果を`State.CaptureFileExists`に設定している

- [x] 派生プロパティが`State`に委譲されている：
  - [x] `CameraButtonVisibility` → `State.CameraButtonVisibility`
  - [x] `IsCheckBoxEnabled` → `State.IsCheckBoxEnabled`

- [x] 状態更新メソッドが実装されている：
  - [x] `UpdateItemState(string newItemState)`
  - [x] `UpdateCaptureFileExists(bool exists)`

- [x] PropertyChanged通知が適切に発火している

- [x] 既存テストがすべてPassしている

- [x] ビルドが成功している（警告なし）

---

## 技術メモ

### ファイル存在チェックの最適化

**変更前（パフォーマンス問題）**:
```csharp
public Visibility CameraButtonVisibility
{
    get
    {
        if (File.Exists(CaptureFilePath))  // ★毎回ディスクI/O発生
            return Visibility.Visible;
        return Visibility.Collapsed;
    }
}
```

**変更後（最適化）**:
```csharp
public CheckItemViewModel(CheckItemEntity entity, WindowMode windowMode)
{
    // コンストラクタで1回だけチェック
    bool captureFileExists = File.Exists(entity.CaptureFilePath);

    State = new CheckItemState(
        windowMode,
        entity.ItemState,
        captureFileExists
    );
}

public Visibility CameraButtonVisibility => State.CameraButtonVisibility;
```

### PropertyChanged通知の設計

状態更新メソッドでは、関連する派生プロパティすべてに通知を発火する必要があります：

```csharp
public void UpdateItemState(string newItemState)
{
    State.ItemState = newItemState;
    OnPropertyChanged(nameof(ItemState));
    OnPropertyChanged(nameof(CameraButtonVisibility));
}
```

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
| 2025-12-01 | 実装完了 - CheckItemViewModel修正、既存33テストPass |
