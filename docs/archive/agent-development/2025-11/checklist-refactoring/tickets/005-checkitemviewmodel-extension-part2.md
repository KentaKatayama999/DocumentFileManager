# チケット #005: CheckItemViewModel拡張（Part 2: 実装）

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 2時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #004
- **タグ**: viewmodel, mvvm, binding

## 概要

チケット#004で作成したテストケースをパスするように、CheckItemViewModelを拡張します。ICommand（CommunityToolkit.Mvvm使用）、プロパティ、初期化ロジックを追加し、MVVMパターンに準拠した設計にします。

## 実装内容

### 1. CheckItemViewModel.cs 拡張

**ファイル**: `src/DocumentFileManager.UI/ViewModels/CheckItemViewModel.cs`

### 2. 新規プロパティ追加

#### 2.1 IsCheckBoxEnabled

```csharp
private bool _isCheckBoxEnabled = true;

/// <summary>
/// チェックボックスの有効/無効状態
/// MainWindowでは無効化、ChecklistWindowでは有効化
/// </summary>
public bool IsCheckBoxEnabled
{
    get => _isCheckBoxEnabled;
    set => SetProperty(ref _isCheckBoxEnabled, value);
}
```

#### 2.2 CameraButtonVisibility

```csharp
/// <summary>
/// カメラボタンの表示/非表示状態
/// </summary>
public Visibility CameraButtonVisibility
{
    get
    {
        if (!HasCapture)
            return Visibility.Collapsed;

        // ファイル存在確認
        if (string.IsNullOrEmpty(CaptureFilePath) || !File.Exists(CaptureFilePath))
            return Visibility.Collapsed;

        return Visibility.Visible;
    }
}
```

### 3. ICommand追加（CommunityToolkit.Mvvm使用）

#### 3.1 CheckedChangedCommand

```csharp
private ICommand _checkedChangedCommand;

/// <summary>
/// チェック状態変更コマンド
/// </summary>
public ICommand CheckedChangedCommand
{
    get => _checkedChangedCommand;
    set => SetProperty(ref _checkedChangedCommand, value);
}
```

**注意**: このCommandは外部から設定されます（CheckItemUIBuilderまたはChecklistWindow）。async voidの問題を回避するため、Command実装側でtry-catchを実装します。

#### 3.2 ViewCaptureCommand

```csharp
private ICommand _viewCaptureCommand;

/// <summary>
/// キャプチャ表示コマンド
/// </summary>
public ICommand ViewCaptureCommand
{
    get => _viewCaptureCommand;
    set => SetProperty(ref _viewCaptureCommand, value);
}
```

### 4. UpdateCaptureButton() メソッド追加

```csharp
/// <summary>
/// キャプチャファイルの存在確認とボタン表示更新
/// </summary>
public void UpdateCaptureButton()
{
    OnPropertyChanged(nameof(HasCapture));
    OnPropertyChanged(nameof(CameraButtonVisibility));
}
```

### 5. コンストラクタ拡張

```csharp
private readonly string _documentRootPath;
private readonly bool _isMainWindow;

public CheckItemViewModel(
    CheckItemEntity entity,
    string documentRootPath,
    bool isMainWindow = false)
{
    Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    _documentRootPath = documentRootPath ?? throw new ArgumentNullException(nameof(documentRootPath));
    _isMainWindow = isMainWindow;

    // IsCheckBoxEnabledの初期化
    IsCheckBoxEnabled = !isMainWindow;

    // IsCheckedの初期化ロジック
    InitializeIsChecked();
}
```

### 6. IsCheckedの初期化ロジック明確化

```csharp
private void InitializeIsChecked()
{
    if (_isMainWindow)
    {
        // MainWindow: 最新キャプチャがあればチェック表示（読み取り専用）
        IsChecked = HasCapture && File.Exists(CaptureFilePath);
    }
    else
    {
        // ChecklistWindow: CheckItemDocumentのIsCheckedから復元
        // または、Entity.Status == CheckItemStatus.Done の場合はtrue
        IsChecked = Entity.Status == CheckItemStatus.Done;
    }
}
```

**注意**: CheckItemDocumentから復元する処理は、CheckItemUIBuilderまたはChecklistWindowで行います（Repository依存を避けるため）。

### 7. IDisposable実装（メモリリーク防止）

```csharp
public void Dispose()
{
    // ICommandの解放
    if (_checkedChangedCommand is IDisposable disposableCheckedCommand)
    {
        disposableCheckedCommand.Dispose();
    }

    if (_viewCaptureCommand is IDisposable disposableViewCommand)
    {
        disposableViewCommand.Dispose();
    }
}
```

または、WeakEventManagerを使用してイベントリークを防ぐ方法も検討します。

### 8. CaptureFilePathのセッター修正

```csharp
private string _captureFilePath;

/// <summary>
/// キャプチャファイルパス
/// </summary>
public string CaptureFilePath
{
    get => _captureFilePath;
    set
    {
        if (SetProperty(ref _captureFilePath, value))
        {
            OnPropertyChanged(nameof(HasCapture));
            OnPropertyChanged(nameof(CameraButtonVisibility));
        }
    }
}
```

## 完了条件（チェックリスト）

- [ ] IsCheckBoxEnabledプロパティが追加されている
- [ ] CameraButtonVisibilityプロパティが追加されている
- [ ] CheckedChangedCommandプロパティが追加されている
- [ ] ViewCaptureCommandプロパティが追加されている
- [ ] UpdateCaptureButton()メソッドが実装されている
- [ ] コンストラクタにdocumentRootPathとisMainWindowパラメータが追加されている
- [ ] InitializeIsChecked()メソッドが実装されている
- [ ] MainWindow用の初期化ロジックが実装されている
- [ ] ChecklistWindow用の初期化ロジックが実装されている
- [ ] Entity.Status == Doneの場合の初期化が実装されている
- [ ] IDisposable実装またはWeakEventManager使用が検討されている
- [ ] CaptureFilePathのセッターでCameraButtonVisibilityが更新される
- [ ] ビルドが成功する
- [ ] チケット#004で作成した全テストケースがパスする

## 技術メモ

### CommunityToolkit.MvvmのRelayCommand使用例（外部設定）

CheckItemUIBuilderまたはChecklistWindowで以下のようにCommandを設定します：

```csharp
viewModel.CheckedChangedCommand = new AsyncRelayCommand(async () =>
{
    try
    {
        if (viewModel.IsChecked)
        {
            await _stateManager.HandleCheckOnAsync(viewModel, document);
        }
        else
        {
            await _stateManager.HandleCheckOffAsync(viewModel, document);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "チェック状態変更中にエラーが発生しました");
        await _dialogService.ShowErrorAsync("エラーが発生しました", "エラー");
    }
});
```

### WeakEventManagerの使用例

```csharp
// PropertyChangedイベントのリークを防ぐ
WeakEventManager<CheckItemViewModel, PropertyChangedEventArgs>.AddHandler(
    this,
    nameof(PropertyChanged),
    OnPropertyChangedHandler
);
```

### ファイル存在確認のキャッシュ（将来的な改善）

```csharp
private bool? _fileExistsCache;

public Visibility CameraButtonVisibility
{
    get
    {
        if (!HasCapture)
            return Visibility.Collapsed;

        // キャッシュがない場合のみチェック
        if (_fileExistsCache == null)
        {
            _fileExistsCache = File.Exists(CaptureFilePath);
        }

        return _fileExistsCache.Value ? Visibility.Visible : Visibility.Collapsed;
    }
}

// CaptureFilePathが変更されたらキャッシュをクリア
private void ClearFileExistsCache()
{
    _fileExistsCache = null;
}
```

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 3
- `src/DocumentFileManager.UI/ViewModels/` - 実装配置先
- CommunityToolkit.Mvvm公式ドキュメント: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
