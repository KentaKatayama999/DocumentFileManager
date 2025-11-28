# チケット #008: ChecklistWindow修正

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 2時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #007
- **タグ**: integration, window, refactoring

## 概要

ChecklistWindowを修正し、ChecklistStateManagerを使用するように統合します。UIの直接更新コードを削除し、ViewModelのバインディングで自動更新されるようにします。

## 実装内容

### 1. ChecklistStateManagerの依存注入

**コンストラクタ修正**:
```csharp
private readonly ChecklistStateManager _stateManager;
private readonly ICheckItemDocumentRepository _repository;
private readonly ILogger<ChecklistWindow> _logger;

public ChecklistWindow(
    ChecklistStateManager stateManager,
    ICheckItemDocumentRepository repository,
    ILogger<ChecklistWindow> logger)
{
    _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    InitializeComponent();
}
```

### 2. PerformCaptureForCheckItem() メソッド修正

**変更前（直接DB操作）**:
```csharp
private async Task PerformCaptureForCheckItem(CheckItemViewModel viewModel, Document document)
{
    // キャプチャ処理...
    var captureFilePath = ...;

    // DB保存
    var checkItemDoc = await _repository.GetByCheckItemAndDocumentAsync(...);
    checkItemDoc.CaptureFilePath = captureFilePath;
    await _repository.UpdateAsync(checkItemDoc);

    // UI更新
    viewModel.CaptureFilePath = captureFilePath;
}
```

**変更後（ChecklistStateManager使用）**:
```csharp
private async Task PerformCaptureForCheckItem(CheckItemViewModel viewModel, Document document)
{
    try
    {
        // キャプチャ処理
        var captureService = App.ServiceProvider.GetRequiredService<IScreenCaptureService>();
        var captureFilePath = await captureService.CaptureScreenAsync(document.Id, viewModel.Entity.Id);

        if (string.IsNullOrEmpty(captureFilePath))
        {
            _logger.LogWarning("キャプチャがキャンセルされました");
            return;
        }

        // ChecklistStateManagerでコミット
        await _stateManager.CommitCaptureAsync(viewModel, document, captureFilePath);

        _logger.LogInformation("キャプチャを保存しました: {FilePath}", captureFilePath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "キャプチャ保存中にエラーが発生しました");
        throw;
    }
}
```

### 3. RefreshCheckItemsAsync() メソッド保持

**現状維持**:
```csharp
private async Task RefreshCheckItemsAsync()
{
    // CheckItemのUI再構築処理
    // バインディングで自動更新されるようになったが、
    // Window再表示時の全体更新に使用
}
```

**注意**: `ChecklistWindow_Activated`イベントハンドラでの呼び出しは維持します。

### 4. UIの直接更新コード削除

**削除対象**:
```csharp
// 削除: チェックボックスの直接更新
checkBox.IsChecked = true;

// 削除: ボタンの直接Visibility変更
button.Visibility = Visibility.Visible;

// 削除: ViewModelへの直接代入（バインディングで自動更新されるため）
// ただし、CommitCaptureAsyncの戻り値として更新は許可
```

**保持する更新**:
```csharp
// OK: ChecklistStateManagerを経由した更新
await _stateManager.CommitCaptureAsync(viewModel, document, captureFilePath);

// OK: Repository経由の読み込み
var checkItemDoc = await _repository.GetByCheckItemAndDocumentAsync(...);
viewModel.IsChecked = checkItemDoc.IsChecked; // 初期化時のみ
```

### 5. CheckItemUIBuilderの使用方法変更

**変更前（イベントハンドラを渡す）**:
```csharp
var uiBuilder = new CheckItemUIBuilder();
var ui = await uiBuilder.BuildAsync(rootViewModel, document, OnCheckBoxChecked, OnCheckBoxUnchecked);
```

**変更後（依存注入されたインスタンスを使用）**:
```csharp
var uiBuilder = App.ServiceProvider.GetRequiredService<CheckItemUIBuilder>();
var ui = await uiBuilder.BuildAsync(rootViewModel, document);
```

### 6. ChecklistWindow_Activated イベントハンドラ保持

```csharp
private async void ChecklistWindow_Activated(object sender, EventArgs e)
{
    // Window再表示時の全体更新
    await RefreshCheckItemsAsync();
}
```

### 7. エラーハンドリング追加

```csharp
private async void OnPerformCapture(object sender, RoutedEventArgs e)
{
    try
    {
        var button = sender as Button;
        var viewModel = button?.DataContext as CheckItemViewModel;

        if (viewModel == null)
        {
            _logger.LogWarning("DataContextがCheckItemViewModelではありません");
            return;
        }

        await PerformCaptureForCheckItem(viewModel, _currentDocument);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "キャプチャ処理中にエラーが発生しました");
        MessageBox.Show("エラーが発生しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

## 完了条件（チェックリスト）

- [ ] コンストラクタにChecklistStateManagerが追加されている
- [ ] PerformCaptureForCheckItem()がChecklistStateManager.CommitCaptureAsyncを使用している
- [ ] UIの直接更新コードが削除されている
- [ ] CheckItemUIBuilderの使用方法が変更されている（依存注入）
- [ ] RefreshCheckItemsAsync()が保持されている
- [ ] ChecklistWindow_Activatedイベントハンドラが保持されている
- [ ] エラーハンドリングが追加されている
- [ ] try-catchでユーザーにエラー通知が実装されている
- [ ] ビルドが成功する
- [ ] 実行時にChecklistWindowが正しく動作する
- [ ] チェックON/OFF時にUIが自動更新される
- [ ] キャプチャ保存時にDBが正しく更新される

## 技術メモ

### 依存注入の取得方法

**App.ServiceProvider経由**:
```csharp
var stateManager = App.ServiceProvider.GetRequiredService<ChecklistStateManager>();
```

**コンストラクタ注入（推奨）**:
```csharp
public ChecklistWindow(ChecklistStateManager stateManager, ...)
{
    _stateManager = stateManager;
    ...
}
```

### バインディングの確認

UIが自動更新されない場合：

1. ViewModelのINotifyPropertyChangedが正しく実装されているか確認
2. バインディングのModeがTwoWayに設定されているか確認
3. DataContextが正しく設定されているか確認
4. Output Windowで「Binding」エラーを確認

### RefreshCheckItemsAsyncの必要性

バインディングで自動更新されるようになっても、以下の理由でRefreshCheckItemsAsyncは必要です：

- Window再表示時に最新のDB状態を反映
- ファイル削除などの外部変更を反映
- ViewModelツリー全体の再構築

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 6
- `src/DocumentFileManager.UI/Windows/ChecklistWindow.xaml.cs` - 修正対象
- `src/DocumentFileManager.UI/Services/ChecklistStateManager.cs` - 依存先
