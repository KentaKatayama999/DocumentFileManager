# チケット #001 - CheckItemState作成（TDD）

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #001 |
| **タイトル** | CheckItemState作成（TDD） |
| **ステータス** | Done |
| **優先度** | High |
| **担当者** | 未割当 |
| **見積時間** | 4-6時間 |
| **実績時間** | 0.5h |
| **作成日** | 2025-11-29 |
| **更新日** | 2025-11-29 |
| **依存チケット** | なし |

---

## 説明

CheckItemUIBuilderのGod Class問題を解消するため、状態管理を担当する`CheckItemState`クラスを新規作成します。TDD方式で実装し、状態パラメータと派生プロパティの計算ロジックを確実に実装します。

このクラスは3つの状態パラメータ（WindowMode, ItemState, CaptureFileExists）を保持し、2つの派生プロパティ（CameraButtonVisibility, IsCheckBoxEnabled）を計算します。

---

## 対象ファイル

### 新規作成
- `src/DocumentFileManager.UI/Models/CheckItemState.cs`
- `tests/DocumentFileManager.Tests/Models/CheckItemStateTests.cs`

---

## タスク一覧

- [ ] **Step 1: 単体テストファイル作成**
  - [ ] `tests/DocumentFileManager.Tests/Models/CheckItemStateTests.cs` 作成
  - [ ] MainWindow×各ItemState×CaptureFileExists組み合わせテスト
  - [ ] ChecklistWindow×各ItemState×CaptureFileExists組み合わせテスト
  - [ ] CameraButtonVisibility計算ロジックテスト
  - [ ] IsCheckBoxEnabled計算ロジックテスト

- [ ] **Step 2: CheckItemStateクラス実装**
  - [ ] WindowMode enum定義（MainWindow=0, ChecklistWindow=1）
  - [ ] 状態パラメータ実装
    - [ ] WindowMode プロパティ
    - [ ] ItemState プロパティ（string型、00/10/11/20/22）
    - [ ] CaptureFileExists プロパティ（bool型）
  - [ ] 派生プロパティ実装
    - [ ] CameraButtonVisibility プロパティ（Visibility型）
      - [ ] MainWindow: CaptureFileExists==true → Visible
      - [ ] ChecklistWindow: ItemState[1]=='1' AND CaptureFileExists==true → Visible
    - [ ] IsCheckBoxEnabled プロパティ（bool型）
      - [ ] WindowMode==ChecklistWindow → true

- [ ] **Step 3: テスト実行・Green確認**
  - [ ] すべての単体テストがPassすることを確認
  - [ ] テストカバレッジ100%を確認

- [ ] **Step 4: コミット**
  - [ ] git add, commit, push
  - [ ] コミットメッセージ: `feat: Phase 1完了 - CheckItemState作成（TDD）`

---

## 受け入れ条件（Acceptance Criteria）

- [x] `CheckItemState.cs`が作成され、以下を実装している：
  - [x] WindowMode enum（MainWindow=0, ChecklistWindow=1）
  - [x] 3つの状態パラメータ（WindowMode, ItemState, CaptureFileExists）
  - [x] 2つの派生プロパティ（CameraButtonVisibility, IsCheckBoxEnabled）

- [x] `CheckItemStateTests.cs`が作成され、以下をテストしている：
  - [x] MainWindow×各ItemState組み合わせ（5パターン×2状態=10テストケース）
  - [x] ChecklistWindow×各ItemState組み合わせ（5パターン×2状態=10テストケース）
  - [x] CameraButtonVisibility計算ロジック
  - [x] IsCheckBoxEnabled計算ロジック

- [x] すべての単体テストがPassしている

- [x] ビルドが成功している（警告なし）

---

## 技術メモ

### ItemState状態コード
```
00 = 未紐づけ
10 = チェックON、キャプチャなし
11 = チェックON、キャプチャあり
20 = チェックOFF（履歴あり）、キャプチャなし
22 = チェックOFF（履歴あり）、キャプチャあり
```

### CameraButtonVisibility分岐ロジック
| WindowMode | 条件 | 結果 |
|------------|------|------|
| MainWindow | CaptureFileExists==true | Visible |
| MainWindow | CaptureFileExists==false | Collapsed |
| ChecklistWindow | ItemState[1]=='1' AND CaptureFileExists==true | Visible |
| ChecklistWindow | 上記以外 | Collapsed |

### IsCheckBoxEnabled分岐ロジック
- MainWindow: チェックボックス無効（表示のみ）
- ChecklistWindow: チェックボックス有効（操作可能）

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
