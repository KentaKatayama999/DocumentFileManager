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
| **更新日** | 2025-11-29 |
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

- [ ] **Step 1: CheckItemStateプロパティ追加**
  - [ ] `public CheckItemState State { get; private set; }` プロパティ追加
  - [ ] コンストラクタで初期化

- [ ] **Step 2: ファイル存在チェック最適化**
  - [ ] コンストラクタで `File.Exists(CaptureFilePath)` を1回実行
  - [ ] 結果を `CheckItemState.CaptureFileExists` に設定
  - [ ] getter内の `File.Exists()` 呼び出しを削除

- [ ] **Step 3: 派生プロパティの委譲**
  - [ ] `CameraButtonVisibility` getter を `State.CameraButtonVisibility` に委譲
  - [ ] `IsCheckBoxEnabled` getter を `State.IsCheckBoxEnabled` に委譲
  - [ ] 既存プロパティとの互換性維持（INotifyPropertyChangedは維持）

- [ ] **Step 4: 状態更新メソッド追加**
  - [ ] `UpdateItemState(string newItemState)` メソッド追加
  - [ ] `UpdateCaptureFileExists(bool exists)` メソッド追加
  - [ ] PropertyChanged通知を適切に発火

- [ ] **Step 5: テスト更新**
  - [ ] 既存テストを更新（CheckItemState導入に対応）
  - [ ] 新規テストケース追加
    - [ ] State初期化テスト
    - [ ] 派生プロパティ委譲テスト
    - [ ] 状態更新メソッドテスト

- [ ] **Step 6: ビルド・テスト実行**
  - [ ] ビルド成功確認
  - [ ] すべてのテストがPass確認

- [ ] **Step 7: コミット**
  - [ ] git add, commit, push
  - [ ] コミットメッセージ: `refactor: Phase 2完了 - CheckItemViewModel修正（State導入）`

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
