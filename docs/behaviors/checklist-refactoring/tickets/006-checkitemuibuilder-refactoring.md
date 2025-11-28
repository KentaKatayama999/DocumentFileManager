# チケット #006: CheckItemUIBuilderリファクタリング

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 4時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #005
- **タグ**: refactoring, ui-builder, separation-of-concerns

## 概要

CheckItemUIBuilderから責務を分離し、UI構築とバインディング設定のみに特化させます。イベントハンドラ内のDB操作、直接的なUI更新をすべて削除し、ChecklistStateManagerとViewModelのバインディングに置き換えます。

## 実装内容

### 1. 責務の明確化

**残す責務**:
- UI構築（GroupBox、CheckBox、Buttonの生成）
- バインディング設定（ViewModel → UI）
- DataContextの設定

**削除する責務**:
- ❌ イベントハンドラ内のDB操作 → ChecklistStateManagerへ移動
- ❌ チェック状態の直接変更 → ViewModelのバインディングで自動化
- ❌ `SaveStatusAsync()` → ChecklistStateManagerへ移動
- ❌ キャプチャファイル確認ダイアログ → IDialogServiceへ移動

### 2. コンストラクタ修正

ChecklistStateManagerを依存注入します：

```csharp
private readonly ChecklistStateManager _stateManager;
private readonly ILogger<CheckItemUIBuilder> _logger;

public CheckItemUIBuilder(
    ChecklistStateManager stateManager,
    ILogger<CheckItemUIBuilder> logger)
{
    _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### 3. CreateCheckBox() メソッド修正

**変更前（イベントハンドラ直接登録）**:
```csharp
checkBox.Checked += async (s, e) =>
{
    // DB操作、ダイアログ表示等...
    await SaveStatusAsync(...);
};
```

**変更後（Commandバインディング）**:
```csharp
private CheckBox CreateCheckBox(CheckItemViewModel viewModel, Document document)
{
    var checkBox = new CheckBox
    {
        Content = viewModel.Label,
        DataContext = viewModel
    };

    // IsCheckedをTwoWayバインディング
    var isCheckedBinding = new Binding(nameof(viewModel.IsChecked))
    {
        Source = viewModel,
        Mode = BindingMode.TwoWay
    };
    checkBox.SetBinding(CheckBox.IsCheckedProperty, isCheckedBinding);

    // IsEnabledをバインディング
    var isEnabledBinding = new Binding(nameof(viewModel.IsCheckBoxEnabled))
    {
        Source = viewModel
    };
    checkBox.SetBinding(CheckBox.IsEnabledProperty, isEnabledBinding);

    // Commandをバインディング（外部から設定済みのCommandを使用）
    var commandBinding = new Binding(nameof(viewModel.CheckedChangedCommand))
    {
        Source = viewModel
    };
    checkBox.SetBinding(CheckBox.CommandProperty, commandBinding);

    return checkBox;
}
```

### 4. CreateButton() メソッド修正

```csharp
private Button CreateButton(CheckItemViewModel viewModel)
{
    var button = new Button
    {
        Content = "📷",
        DataContext = viewModel
    };

    // Visibilityをバインディング
    var visibilityBinding = new Binding(nameof(viewModel.CameraButtonVisibility))
    {
        Source = viewModel
    };
    button.SetBinding(Button.VisibilityProperty, visibilityBinding);

    // Commandをバインディング
    var commandBinding = new Binding(nameof(viewModel.ViewCaptureCommand))
    {
        Source = viewModel
    };
    button.SetBinding(Button.CommandProperty, commandBinding);

    return button;
}
```

### 5. BuildViewModelHierarchy() でCommandを設定

ViewModelにChecklistStateManager呼び出しロジックを注入します：

```csharp
private void SetupCommands(CheckItemViewModel viewModel, Document document)
{
    // CheckedChangedCommand設定
    viewModel.CheckedChangedCommand = new AsyncRelayCommand(async () =>
    {
        try
        {
            if (viewModel.IsChecked)
            {
                var transition = await _stateManager.HandleCheckOnAsync(viewModel, document);

                // キャプチャ取得確認
                if (transition.ShouldPromptForCapture)
                {
                    // キャプチャ処理はChecklistWindowで実装
                    // ここでは状態遷移のみ実施
                }
            }
            else
            {
                await _stateManager.HandleCheckOffAsync(viewModel, document);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック状態変更中にエラーが発生しました");
            throw;
        }
    });

    // ViewCaptureCommand設定
    viewModel.ViewCaptureCommand = new RelayCommand(() =>
    {
        // CaptureImageViewerWindowを開く処理
        // （ChecklistWindowから移植）
    });
}
```

### 6. SaveStatusAsync() メソッド削除

このメソッドはChecklistStateManager.CommitTransitionAsyncに置き換えられます。

### 7. 削除対象のイベントハンドラ

```csharp
// 削除: Checked/Uncheckedイベントハンドラ
checkBox.Checked -= OnCheckBoxChecked;
checkBox.Unchecked -= OnCheckBoxUnchecked;

// 削除: private async void OnCheckBoxChecked(...)
// 削除: private async void OnCheckBoxUnchecked(...)
```

### 8. テストケース作成

**ファイル**: `src/DocumentFileManager.UI.Tests/Helpers/CheckItemUIBuilderTests.cs`

**テストケース**:
- `BuildAsync_ルート項目生成_GroupBox作成確認`
- `BuildAsync_チェック項目生成_CheckBox作成確認`
- `CreateCheckBox_ViewModelバインディング設定確認`
- `CreateCheckBox_Commandバインディング設定確認`
- `CreateButton_Visibilityバインディング設定確認`

## 完了条件（チェックリスト）

- [ ] CheckItemUIBuilderTests.csが作成されている
- [ ] テストケース5つが作成されている
- [ ] コンストラクタにChecklistStateManagerが追加されている
- [ ] CreateCheckBox()でIsCheckedがTwoWayバインディングされている
- [ ] CreateCheckBox()でIsEnabledがバインディングされている
- [ ] CreateCheckBox()でCommandがバインディングされている
- [ ] CreateButton()でVisibilityがバインディングされている
- [ ] CreateButton()でCommandがバインディングされている
- [ ] BuildViewModelHierarchy()でCommandが設定されている
- [ ] CheckedChangedCommandにtry-catchが実装されている
- [ ] SaveStatusAsync()メソッドが削除されている
- [ ] Checked/Uncheckedイベントハンドラが削除されている
- [ ] ビルドが成功する
- [ ] すべてのテストケースがパスする

## 技術メモ

### バインディングのベストプラクティス

```csharp
// TwoWayバインディングの設定例
var binding = new Binding
{
    Path = new PropertyPath(nameof(viewModel.IsChecked)),
    Source = viewModel,
    Mode = BindingMode.TwoWay,
    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
};
checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
```

### Commandのエラーハンドリング

```csharp
viewModel.CheckedChangedCommand = new AsyncRelayCommand(async () =>
{
    try
    {
        // ビジネスロジック
    }
    catch (OperationCanceledException)
    {
        // ユーザーキャンセル時は無視
        _logger.LogInformation("ユーザーが操作をキャンセルしました");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "予期しないエラーが発生しました");
        throw; // 上位レイヤーで処理
    }
});
```

### DataContextの設定

```csharp
// CheckBoxとButtonに同じViewModelを設定
checkBox.DataContext = viewModel;
button.DataContext = viewModel;

// バインディングでSourceを明示的に指定する方が安全
var binding = new Binding(nameof(viewModel.IsChecked))
{
    Source = viewModel, // DataContextに依存しない
    Mode = BindingMode.TwoWay
};
```

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 4
- `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs` - リファクタリング対象
- `src/DocumentFileManager.UI/Services/ChecklistStateManager.cs` - 依存先
