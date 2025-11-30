# チケット一覧

**最終更新**: 2025-11-29

---

## 📊 全体進捗

| 項目 | 値 |
|-----|-----|
| **総チケット数** | 6 |
| **完了** | 5 (83%) |
| **レビュー中** | 1 (17%) |
| **未着手** | 0 (0%) |
| **ブロック中** | 0 (0%) |

### 見積工数
- **総見積時間**: 28-40時間
- **実績時間**: 2.5時間
- **残工数**: 0時間（#005は後続実装予定）

---

## 🎯 プロジェクト概要

**プロジェクト名**: CheckItem状態ベースリファクタリング

**目的**: CheckItemUIBuilderのGod Class問題を解消し、状態ベースの管理でコードをシンプル化する

**戦略**: 6フェーズの段階的リファクタリング

**参照ドキュメント**:
- [リファクタリング計画書](../design/checkitem-state-refactoring-plan.md)
- [現状分析書](../analysis/checkitem-current-state-analysis.md)

---

## 📋 チケット一覧

### Phase 1: 基盤実装（High Priority）

| 番号 | タイトル | ステータス | 優先度 | 見積時間 | 実績時間 | 依存 |
|-----|---------|----------|--------|---------|---------|-----|
| [#001](001-checkitem-state-creation.md) | CheckItemState作成（TDD） | Done | High | 4-6h | 0.5h | - |
| [#002](002-checkitem-viewmodel-modification.md) | CheckItemViewModel修正 | Done | High | 4-6h | 0.5h | #001 |
| [#003](003-checkitem-viewmodel-factory-creation.md) | CheckItemViewModelFactory作成 | Done | High | 4-6h | 0.5h | #002 |

**Phase 1 小計**: 12-18時間

---

### Phase 2: リファクタリング実装（Medium Priority）

| 番号 | タイトル | ステータス | 優先度 | 見積時間 | 実績時間 | 依存 |
|-----|---------|----------|--------|---------|---------|-----|
| [#004](004-checkitem-uibuilder-refactoring.md) | CheckItemUIBuilder縮小リファクタリング | Done | Medium | 6-8h | 0.5h | #003 |
| [#005](005-window-command-setup.md) | Window側コマンド設定実装 | Review | Medium | 6-8h | - | #004 |

**Phase 2 小計**: 12-16時間

---

### Phase 3: テスト・検証（High Priority）

| 番号 | タイトル | ステータス | 優先度 | 見積時間 | 実績時間 | 依存 |
|-----|---------|----------|--------|---------|---------|-----|
| [#006](006-integration-testing.md) | 統合テスト・動作確認 | Done | High | 4-6h | 0.5h | #005 |

**Phase 3 小計**: 4-6時間

---

## 📈 ステータス別サマリー

| ステータス | 件数 | 割合 |
|----------|-----|------|
| Open（未着手） | 6 | 100% |
| In Progress（進行中） | 0 | 0% |
| Review（レビュー待ち） | 0 | 0% |
| Done（完了） | 0 | 0% |
| Blocked（ブロック中） | 0 | 0% |

---

## 🎯 優先度別サマリー

| 優先度 | 件数 | 見積時間 |
|--------|-----|---------|
| High | 4 | 16-24h |
| Medium | 2 | 12-16h |
| Low | 0 | 0h |

---

## 🔗 依存関係グラフ

```
#001 CheckItemState作成（TDD）
  ↓
#002 CheckItemViewModel修正
  ↓
#003 CheckItemViewModelFactory作成
  ↓
#004 CheckItemUIBuilder縮小リファクタリング
  ↓
#005 Window側コマンド設定実装
  ↓
#006 統合テスト・動作確認
```

---

## 📝 実装フェーズ詳細

### Phase 1: 基盤実装（12-18時間）

**目的**: 状態管理の基盤となるCheckItemStateとFactoryパターンを実装

**成果物**:
- CheckItemStateクラス（状態管理）
- CheckItemViewModelFactoryクラス（Entity→ViewModel変換）
- 修正されたCheckItemViewModel（State保持、派生プロパティ委譲）

**完了条件**:
- すべての単体テストがPass
- ビルド成功（警告なし）
- ファイル存在チェックがコンストラクタで1回のみ実行される

---

### Phase 2: リファクタリング実装（12-16時間）

**目的**: God Classを解消し、責務を分離

**成果物**:
- 縮小されたCheckItemUIBuilder（UI生成のみ）
- Window側コマンド設定（MainWindow, ChecklistWindow）
- コールバック方式の廃止

**完了条件**:
- CheckItemUIBuilderが200行以下に縮小
- コマンド設定がWindow側に移動
- ビルド成功（警告なし）

---

### Phase 3: テスト・検証（4-6時間）

**目的**: リファクタリングの完全性を検証

**成果物**:
- 統合テスト結果レポート
- パフォーマンス測定結果
- リグレッションテスト結果

**完了条件**:
- すべてのテストシナリオでPass
- 既存機能にリグレッションなし
- パフォーマンス改善効果確認（ファイル存在チェック最適化）

---

## 🚀 次のアクション

### 即座に開始可能
- [#001 CheckItemState作成（TDD）](001-checkitem-state-creation.md)
  - 依存チケットなし
  - 優先度: High
  - 見積時間: 4-6時間

### 開始準備
1. テスト環境セットアップ確認
2. workspace.dbバックアップ
3. feature/state-based-checkitemブランチで作業

---

## 📚 関連ドキュメント

- [CheckItem状態ベースリファクタリング計画](../design/checkitem-state-refactoring-plan.md)
- [CheckItem関連コードの現状分析](../analysis/checkitem-current-state-analysis.md)
- [HANDOFF.md](../../HANDOFF.md) - セッションハンドオフドキュメント

---

## 📖 チケット管理ルール

### ステータス定義

| ステータス | 説明 |
|----------|------|
| Open | 未着手（開始可能） |
| In Progress | 実装中 |
| Review | レビュー待ち |
| Done | 完了 |
| Blocked | ブロック中（依存チケット未完了等） |

### ステータス遷移

```
Open → In Progress → Review → Done
          ↓            ↑
       Blocked ────────┘
```

### 更新ルール

1. チケット開始時: ステータスを`In Progress`に変更
2. 実装完了時: ステータスを`Review`に変更、実績時間を記録
3. レビュー完了時: ステータスを`Done`に変更
4. ブロック発生時: ステータスを`Blocked`に変更、理由を記載
5. 各チケット更新時: このindex.mdも更新

---

**作成日**: 2025-11-29
**最終更新**: 2025-11-29
**管理者**: プロジェクトチーム
