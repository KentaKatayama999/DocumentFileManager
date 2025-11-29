# MVVMリファクタリング計画

## 概要

現在の中途半端なMVVM実装を、きちんとしたMVVMパターンに統一する。

## 現状の問題点

1. **CheckBoxイベントハンドラとICommandの二重構造**
2. **IsProcessingCheckChangeフラグによる複雑性**
3. **PerformCaptureForCheckItemでのViewModel直接更新**
4. **CameraButtonVisibilityでのFile.Exists毎回呼び出し**

## 新設計

### アーキテクチャ

```
[MainWindow.xaml / ChecklistWindow.xaml]
    ↓ DataTemplate + Binding
[CheckItemViewModel]
    ├── IsChecked (TwoWay Binding) - 表示専用、セッターで状態変更しない
    ├── CameraButtonVisibility (OneWay Binding)
    ├── SelectCommand (MainWindow用: クリックで資料フィルタリング)
    ├── CheckedChangedCommand (ChecklistWindow用: チェック状態変更処理)
    └── ViewCaptureCommand (カメラボタンクリック)
    ↓
[IChecklistStateManager]
    ↓
[ICheckItemDocumentRepository]
```

### CheckItemViewModel の設計

```csharp
public class CheckItemViewModel : INotifyPropertyChanged
{
    // === 基本プロパティ ===
    public CheckItem Entity { get; }
    public int Id => Entity.Id;
    public string Label => Entity.Label;
    public string Path => Entity.Path;
    public string DocumentRootPath { get; }
    public bool IsMainWindow { get; }

    // === 状態プロパティ ===
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CameraButtonVisibility));
            }
        }
    }

    private string? _captureFilePath;
    public string? CaptureFilePath
    {
        get => _captureFilePath;
        set
        {
            if (_captureFilePath != value)
            {
                _captureFilePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCapture));
                OnPropertyChanged(nameof(CameraButtonVisibility));
            }
        }
    }

    public bool HasCapture => !string.IsNullOrEmpty(_captureFilePath);

    // === 表示プロパティ ===
    public bool IsCheckBoxEnabled => !IsMainWindow;

    public Visibility CameraButtonVisibility
    {
        get
        {
            if (!HasCapture) return Visibility.Collapsed;
            if (!IsMainWindow && !IsChecked) return Visibility.Collapsed;
            return Visibility.Visible;
        }
    }

    // === コマンド ===
    // MainWindow用: クリックで資料フィルタリング
    public ICommand? SelectCommand { get; set; }

    // ChecklistWindow用: チェック状態変更処理
    public IAsyncRelayCommand? CheckedChangedCommand { get; set; }

    // カメラボタンクリック
    public ICommand? ViewCaptureCommand { get; set; }

    // === 削除するプロパティ ===
    // IsProcessingCheckChange - 不要になる
}
```

### コマンドの流れ

#### MainWindow: SelectCommand
```
ユーザーがCheckBoxクリック
    ↓
SelectCommand.Execute()
    ↓
MainWindow.FilterDocumentsByCheckItem(viewModel)
    ↓
資料リストをフィルタリング表示
（IsCheckedは変更しない）
```

#### ChecklistWindow: CheckedChangedCommand
```
ユーザーがCheckBoxクリック
    ↓
IsChecked が TwoWay Binding で変更
    ↓
CheckedChangedCommand.Execute() ← CheckBox.Command で発火
    ↓
ChecklistStateManager.HandleCheckOnAsync/HandleCheckOffAsync
    ↓
状態遷移処理 + ダイアログ表示
    ↓
必要に応じてキャプチャ取得ダイアログ
    ↓
DB保存
    ↓
ViewModel.CaptureFilePath 更新
    ↓
PropertyChanged → CameraButtonVisibility 更新
    ↓
UIに反映
```

### CheckItemUIBuilder の変更

```csharp
// 削除するもの
- checkBox.Checked イベントハンドラ
- checkBox.Unchecked イベントハンドラ
- IsProcessingCheckChange の参照

// 追加するもの
- CheckBox.Command バインディング
```

### ファイル変更一覧

| ファイル | 変更内容 |
|---------|---------|
| CheckItemViewModel.cs | IsProcessingCheckChange削除、コマンド整理 |
| CheckItemUIBuilder.cs | イベントハンドラ削除、Commandバインディング追加 |
| ChecklistWindow.xaml.cs | PerformCaptureForCheckItem をViewModel経由に変更 |
| MainWindow.xaml.cs | CheckBox_Click をSelectCommand経由に変更 |
| ChecklistStateManager.cs | キャプチャ保存処理を追加 |

## 実装順序

### Phase 1: CheckItemViewModel の整理
1. IsProcessingCheckChange プロパティを削除
2. SelectCommand プロパティを追加
3. デバッグ用Console.WriteLineを削除

### Phase 2: CheckItemUIBuilder の整理
1. Checked/Unchecked イベントハンドラを削除
2. CheckBox.Command バインディングを追加
3. MainWindow用とChecklistWindow用でコマンドを切り替え

### Phase 3: MainWindow の整理
1. CheckBox_Click イベントハンドラを削除
2. SelectCommand でフィルタリング処理を実行

### Phase 4: ChecklistWindow の整理
1. PerformCaptureForCheckItem の処理をViewModel/StateManager経由に変更
2. キャプチャ保存後のViewModel更新をStateManager内で完結

### Phase 5: テスト
1. 既存のユニットテストが通ることを確認
2. 手動テスト（9シナリオ）を実施

## 注意事項

- CheckBoxのIsCheckedバインディングはTwoWayのまま維持
- MainWindowではIsCheckedを変更しない（表示専用）
- ChecklistWindowではIsCheckedの変更後にコマンドを実行
- File.Exists チェックは CaptureFilePath 設定時に行い、結果をキャッシュ
