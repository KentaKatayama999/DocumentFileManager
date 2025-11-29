# チケット #003: ChecklistStateManager実装（Part 2: 実装）

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 2時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #002
- **タグ**: state-management, business-logic

## 概要

チケット#002で作成したテストケースをパスするように、ChecklistStateManagerの実装を行います。チェックボックスクリック時の状態遷移ロジックとDB操作を担当する中核クラスです。

## 実装内容

### 1. ChecklistStateManager.cs 実装

**ファイル**: `src/DocumentFileManager.UI/Services/ChecklistStateManager.cs`

### 2. コンストラクタ

依存関係を注入します：

```csharp
private readonly ICheckItemDocumentRepository _repository;
private readonly IDialogService _dialogService;
private readonly ILogger<ChecklistStateManager> _logger;

public ChecklistStateManager(
    ICheckItemDocumentRepository repository,
    IDialogService dialogService,
    ILogger<ChecklistStateManager> logger)
{
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### 3. 主要メソッド実装

#### 3.1 HandleCheckOnAsync

**責務**: チェックON時の処理

**実装内容**:
1. 既存キャプチャの確認（ConfirmRestoreExistingCaptureAsync）
2. ユーザーの選択に応じた状態遷移（復帰/破棄/キャンセル）
3. キャプチャ取得確認ダイアログ（IDialogService.ShowConfirmationAsync）
4. 状態遷移オブジェクト（CheckItemTransition）の生成
5. try-catchでエラーハンドリング

```csharp
public async Task<CheckItemTransition> HandleCheckOnAsync(
    CheckItemViewModel viewModel,
    Document document)
{
    try
    {
        // 既存キャプチャの確認
        var restoreResult = await ConfirmRestoreExistingCaptureAsync(viewModel, document);

        // ユーザーの選択に応じた処理...

        return transition;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "チェックON処理中にエラーが発生しました");
        throw;
    }
}
```

#### 3.2 HandleCheckOffAsync

**責務**: チェックOFF時の処理

**実装内容**:
1. キャプチャの有無を確認
2. 状態遷移（キャプチャあり→状態22、なし→状態20）
3. try-catchでエラーハンドリング

```csharp
public async Task<CheckItemTransition> HandleCheckOffAsync(
    CheckItemViewModel viewModel,
    Document document)
{
    try
    {
        var transition = CheckItemTransition.TransitionToOff(
            viewModel,
            document
        );

        await CommitTransitionAsync(transition);

        return transition;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "チェックOFF処理中にエラーが発生しました");
        throw;
    }
}
```

#### 3.3 ConfirmRestoreExistingCaptureAsync

**責務**: 既存キャプチャ復帰の確認

**実装内容**:
1. CheckItemDocumentレポジトリから既存レコード取得
2. キャプチャファイルの存在確認
3. IDialogService.ShowYesNoCancelAsyncで確認ダイアログ表示
4. RestoreResult（復帰/破棄/キャンセル）を返却

```csharp
public async Task<RestoreResult> ConfirmRestoreExistingCaptureAsync(
    CheckItemViewModel viewModel,
    Document document)
{
    // 既存レコード取得
    var existingDoc = await _repository.GetByCheckItemAndDocumentAsync(
        viewModel.Entity.Id,
        document.Id
    );

    if (existingDoc?.CaptureFilePath == null)
    {
        return RestoreResult.NoCapture;
    }

    // ファイル存在確認
    if (!File.Exists(existingDoc.CaptureFilePath))
    {
        return RestoreResult.NoCapture;
    }

    // 確認ダイアログ
    var result = await _dialogService.ShowYesNoCancelAsync(
        "既存のキャプチャ画像が見つかりました。復帰しますか？",
        "キャプチャ復帰確認"
    );

    return result switch
    {
        DialogResult.Yes => RestoreResult.Restore,
        DialogResult.No => RestoreResult.Discard,
        DialogResult.Cancel => RestoreResult.Cancel,
        _ => RestoreResult.Cancel
    };
}
```

#### 3.4 CommitCaptureAsync

**責務**: キャプチャ保存後の処理

**実装内容**:
1. CheckItemTransitionを状態11に更新
2. DB保存（CommitTransitionAsync）
3. ViewModelのCaptureFilePathを更新

#### 3.5 CommitTransitionAsync

**責務**: 状態をDBにコミット

**実装内容**:
1. 新規レコード作成（transition.RequiresNewRecord = true）
2. 既存レコード更新（transition.RequiresNewRecord = false）
3. ICheckItemDocumentRepository呼び出し

```csharp
public async Task CommitTransitionAsync(CheckItemTransition transition)
{
    if (transition.RequiresNewRecord)
    {
        // 新規レコード作成
        var newDoc = new CheckItemDocument
        {
            CheckItemId = transition.ViewModel.Entity.Id,
            DocumentId = transition.Document.Id,
            IsChecked = transition.NewIsChecked,
            CaptureFilePath = transition.NewCaptureFilePath,
            CheckedAt = DateTime.Now
        };

        await _repository.AddAsync(newDoc);
    }
    else
    {
        // 既存レコード更新
        var existingDoc = await _repository.GetByCheckItemAndDocumentAsync(
            transition.ViewModel.Entity.Id,
            transition.Document.Id
        );

        if (existingDoc != null)
        {
            existingDoc.IsChecked = transition.NewIsChecked;
            existingDoc.CaptureFilePath = transition.NewCaptureFilePath;
            existingDoc.CheckedAt = DateTime.Now;

            await _repository.UpdateAsync(existingDoc);
        }
    }
}
```

#### 3.6 RollbackTransitionAsync

**責務**: 状態をロールバック

**実装内容**:
1. ViewModelの状態を元に戻す
2. DB更新は行わない（未コミットのため）

```csharp
public async Task RollbackTransitionAsync(CheckItemTransition transition)
{
    transition.ViewModel.IsChecked = transition.OldIsChecked;
    transition.ViewModel.CaptureFilePath = transition.OldCaptureFilePath;

    _logger.LogInformation("状態遷移をロールバックしました");

    await Task.CompletedTask;
}
```

### 4. AppInitializer.cs にサービス登録

```csharp
services.AddScoped<ChecklistStateManager>();
```

### 5. RestoreResult enum定義

```csharp
public enum RestoreResult
{
    NoCapture,   // キャプチャなし
    Restore,     // 復帰
    Discard,     // 破棄
    Cancel       // キャンセル
}
```

## 完了条件（チェックリスト）

- [x] ChecklistStateManager.csファイルが作成されている
- [x] コンストラクタで依存関係が注入されている
- [x] HandleCheckOnAsyncが実装されている
- [x] HandleCheckOffAsyncが実装されている
- [x] ConfirmRestoreExistingCaptureAsync（HandleCheckOnAsync内に統合）
- [x] CommitCaptureAsyncが実装されている
- [x] CommitTransitionAsyncが実装されている
- [x] RollbackTransitionAsyncが実装されている
- [x] RestoreResult enum（DialogResultを使用）
- [x] try-catchでエラーハンドリングが実装されている
- [x] ILoggerでログ出力が実装されている
- [x] AppInitializer.csにサービス登録が追加されている
- [x] ビルドが成功する
- [x] チケット#002で作成した全テストケースがパスする（10件）

## 技術メモ

### エラーハンドリングのベストプラクティス

```csharp
try
{
    // ビジネスロジック
}
catch (Exception ex)
{
    _logger.LogError(ex, "処理中にエラーが発生: {Message}", ex.Message);

    // ユーザーにエラー通知
    await _dialogService.ShowErrorAsync(
        "処理中にエラーが発生しました。",
        "エラー"
    );

    throw; // 上位レイヤーで再処理させる
}
```

### 状態遷移の設計

CheckItemTransitionクラスを活用して、DBコミット前の一時状態を管理します。これにより、ユーザーがキャンセルした場合のロールバックが容易になります。

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 2
- `src/DocumentFileManager.UI/Services/` - 実装配置先
- `src/DocumentFileManager.UI/Models/CheckItemTransition.cs` - 状態遷移モデル
