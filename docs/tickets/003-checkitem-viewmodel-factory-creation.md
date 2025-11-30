# チケット #003 - CheckItemViewModelFactory作成

> **📖 実装前に必ず確認**: [チケット管理ガイド](~/.claude/docs/tickets/README.md) を参照してください。
> ワークフロー、Review Agent活用、ステータス管理ルールが記載されています。

---

## メタデータ

| 項目 | 内容 |
|-----|------|
| **チケット番号** | #003 |
| **タイトル** | CheckItemViewModelFactory作成 |
| **ステータス** | Done |
| **優先度** | High |
| **担当者** | 未割当 |
| **見積時間** | 4-6時間 |
| **実績時間** | 0.5h |
| **作成日** | 2025-11-29 |
| **更新日** | 2025-12-01 |
| **依存チケット** | #002 |

---

## 説明

Entity（ドメインモデル）からViewModel変換を担当する`CheckItemViewModelFactory`を新規作成します。これにより、CheckItemUIBuilderからViewModel構築ロジックを分離し、責務を明確化します。

Factoryパターンを採用することで、ViewModel生成ロジックの再利用性とテスタビリティを向上させます。

---

## 対象ファイル

### 新規作成
- `src/DocumentFileManager.UI/Factories/CheckItemViewModelFactory.cs`
- `tests/DocumentFileManager.Tests/Factories/CheckItemViewModelFactoryTests.cs`

### 修正
- `src/DocumentFileManager.UI/AppInitializer.cs`（DI登録）

---

## タスク一覧

- [x] **Step 1: Factoryクラス作成**
  - [x] `Factories/CheckItemViewModelFactory.cs` 作成
  - [x] インターフェース定義: `ICheckItemViewModelFactory`
  - [x] 実装クラス: `CheckItemViewModelFactory`

- [x] **Step 2: 変換メソッド実装**
  - [x] `Create(CheckItemEntity entity, WindowMode windowMode)` メソッド
  - [x] Entity → ViewModel変換ロジック
  - [x] CheckItemState初期化
  - [x] 階層構造の再現（親子関係）

- [x] **Step 3: 階層構造対応**
  - [x] `CreateHierarchy(IEnumerable<CheckItemEntity> entities, WindowMode windowMode)` メソッド
  - [x] ルート要素の抽出
  - [x] 子要素の再帰的変換
  - [x] ObservableCollection<CheckItemViewModel>への変換

- [x] **Step 4: DI登録**
  - [x] `AppInitializer.cs`の`ConfigureServices`メソッドに追加
  - [x] `services.AddSingleton<ICheckItemViewModelFactory, CheckItemViewModelFactory>()`

- [x] **Step 5: 単体テスト作成**
  - [x] `CheckItemViewModelFactoryTests.cs` 作成
  - [x] 単一Entity変換テスト
  - [x] 階層構造変換テスト
  - [x] WindowMode別変換テスト
  - [x] null/空リスト処理テスト

- [x] **Step 6: テスト実行**
  - [x] すべての単体テストがPass確認
  - [x] ビルド成功確認

- [x] **Step 7: コミット**
  - [x] git add, commit, push
  - [x] コミットメッセージ: `feat: Phase 3完了 - CheckItemViewModelFactory作成`

---

## 受け入れ条件（Acceptance Criteria）

- [x] `CheckItemViewModelFactory.cs`が作成され、以下を実装している：
  - [x] `ICheckItemViewModelFactory`インターフェース
  - [x] `Create(CheckItemEntity, WindowMode)` メソッド
  - [x] `CreateHierarchy(IEnumerable<CheckItemEntity>, WindowMode)` メソッド

- [x] Entity → ViewModel変換が正しく動作する：
  - [x] すべてのプロパティが正しくマッピングされる
  - [x] CheckItemStateが適切に初期化される
  - [x] 階層構造が再現される

- [x] DIに登録されている（AppInitializer.cs）

- [x] 単体テストが作成され、すべてPassしている：
  - [x] 単一Entity変換テスト
  - [x] 階層構造変換テスト
  - [x] WindowMode別テスト
  - [x] エッジケーステスト

- [x] ビルドが成功している（警告なし）

---

## 技術メモ

### Factoryパターンの利点

1. **責務分離**: ViewModel生成ロジックをBuilderから分離
2. **再利用性**: 複数箇所からViewModel生成可能
3. **テスタビリティ**: Factoryのみを単体テスト可能
4. **依存性注入**: DIコンテナで管理可能

### 階層構造の再現アルゴリズム

```csharp
public ObservableCollection<CheckItemViewModel> CreateHierarchy(
    IEnumerable<CheckItemEntity> entities, WindowMode windowMode)
{
    // 1. ルート要素（ParentId==null）を抽出
    var rootEntities = entities.Where(e => e.ParentId == null);

    // 2. 各ルート要素に対して再帰的に子要素を構築
    var viewModels = new ObservableCollection<CheckItemViewModel>();
    foreach (var rootEntity in rootEntities)
    {
        var viewModel = CreateWithChildren(rootEntity, entities, windowMode);
        viewModels.Add(viewModel);
    }

    return viewModels;
}

private CheckItemViewModel CreateWithChildren(
    CheckItemEntity entity,
    IEnumerable<CheckItemEntity> allEntities,
    WindowMode windowMode)
{
    var viewModel = Create(entity, windowMode);

    // 子要素を再帰的に構築
    var children = allEntities.Where(e => e.ParentId == entity.Id);
    foreach (var child in children)
    {
        var childViewModel = CreateWithChildren(child, allEntities, windowMode);
        viewModel.Children.Add(childViewModel);
    }

    return viewModel;
}
```

### DI登録例

```csharp
// AppInitializer.cs
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // ... 既存の登録 ...

    // CheckItemViewModelFactory登録
    services.AddSingleton<ICheckItemViewModelFactory, CheckItemViewModelFactory>();

    return services.BuildServiceProvider();
}
```

---

## 変更履歴

| 日時 | 変更内容 |
|------|---------|
| 2025-11-29 | チケット作成 |
| 2025-12-01 | 実装完了 - CheckItemViewModelFactory作成、13テストPass、DI登録完了 |
