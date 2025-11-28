# チケット #007: CheckItemControl（DataTemplate）作成

## 基本情報

- **ステータス**: Open
- **優先度**: Medium
- **見積もり**: 2時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #006
- **タグ**: xaml, datatemplate, ui

## 概要

CheckBoxとカメラボタンを含むUIコントロールをDataTemplateとして定義します。これにより、コードビハインドでUI生成コードが不要になり、XAMLでデザイン可能になります。

## 実装内容

### 1. DataTemplate作成

**ファイル**: `src/DocumentFileManager.UI/Windows/ChecklistWindow.xaml` または `App.xaml`

**配置先**: Window.ResourcesまたはApplication.Resources

```xml
<Window.Resources>
    <!-- CheckItemのDataTemplate -->
    <DataTemplate x:Key="CheckItemTemplate">
        <StackPanel Orientation="Horizontal" Margin="2">
            <!-- チェックボックス -->
            <CheckBox Content="{Binding Label}"
                      IsChecked="{Binding IsChecked, Mode=TwoWay}"
                      IsEnabled="{Binding IsCheckBoxEnabled}"
                      Command="{Binding CheckedChangedCommand}"
                      Margin="0,0,5,0" />

            <!-- カメラボタン -->
            <Button Content="📷"
                    Visibility="{Binding CameraButtonVisibility}"
                    Command="{Binding ViewCaptureCommand}"
                    Width="30"
                    Height="25"
                    ToolTip="キャプチャ画像を表示" />
        </StackPanel>
    </DataTemplate>
</Window.Resources>
```

### 2. CheckItemUIBuilderからDataTemplateを使用

**変更前（手動でUI生成）**:
```csharp
private CheckBox CreateCheckBox(...)
{
    var checkBox = new CheckBox { ... };
    // バインディング設定...
    return checkBox;
}

private Button CreateButton(...)
{
    var button = new Button { ... };
    // バインディング設定...
    return button;
}
```

**変更後（DataTemplateを適用）**:
```csharp
private UIElement CreateCheckItemView(CheckItemViewModel viewModel)
{
    var dataTemplate = Application.Current.FindResource("CheckItemTemplate") as DataTemplate;
    if (dataTemplate == null)
    {
        _logger.LogWarning("CheckItemTemplate が見つかりません。デフォルトのUIを生成します。");
        return CreateFallbackUI(viewModel); // フォールバック
    }

    var contentPresenter = new ContentPresenter
    {
        Content = viewModel,
        ContentTemplate = dataTemplate
    };

    return contentPresenter;
}

private UIElement CreateFallbackUI(CheckItemViewModel viewModel)
{
    // DataTemplateが見つからない場合のフォールバック
    // 従来のCreateCheckBox()とCreateButton()を使用
    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
    stackPanel.Children.Add(CreateCheckBox(viewModel));
    stackPanel.Children.Add(CreateButton(viewModel));
    return stackPanel;
}
```

### 3. GroupBoxのItemsControlへの移行（オプション）

**将来的な改善**: GroupBoxのChildrenを手動管理する代わりに、ItemsControlを使用してViewModelコレクションをバインドします。

```xml
<GroupBox Header="{Binding Label}">
    <ItemsControl ItemsSource="{Binding Children}"
                  ItemTemplate="{StaticResource CheckItemTemplate}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Vertical" />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
    </ItemsControl>
</GroupBox>
```

**注意**: この変更は大規模なリファクタリングになるため、将来のチケットで対応します。

### 4. スタイル定義（オプション）

```xml
<Window.Resources>
    <!-- CheckBoxのスタイル -->
    <Style x:Key="CheckItemCheckBoxStyle" TargetType="CheckBox">
        <Setter Property="Margin" Value="0,0,5,0" />
        <Setter Property="VerticalAlignment" Value="Center" />
    </Style>

    <!-- カメラボタンのスタイル -->
    <Style x:Key="CameraButtonStyle" TargetType="Button">
        <Setter Property="Width" Value="30" />
        <Setter Property="Height" Value="25" />
        <Setter Property="Margin" Value="0,0,5,0" />
        <Setter Property="ToolTip" Value="キャプチャ画像を表示" />
    </Style>

    <!-- DataTemplate（スタイル適用版） -->
    <DataTemplate x:Key="CheckItemTemplate">
        <StackPanel Orientation="Horizontal" Margin="2">
            <CheckBox Content="{Binding Label}"
                      IsChecked="{Binding IsChecked, Mode=TwoWay}"
                      IsEnabled="{Binding IsCheckBoxEnabled}"
                      Command="{Binding CheckedChangedCommand}"
                      Style="{StaticResource CheckItemCheckBoxStyle}" />

            <Button Content="📷"
                    Visibility="{Binding CameraButtonVisibility}"
                    Command="{Binding ViewCaptureCommand}"
                    Style="{StaticResource CameraButtonStyle}" />
        </StackPanel>
    </DataTemplate>
</Window.Resources>
```

### 5. 階層表示のスタイル（オプション）

階層深度に応じてインデントを設定：

```xml
<DataTemplate x:Key="CheckItemTemplate">
    <StackPanel Orientation="Horizontal"
                Margin="{Binding Depth, Converter={StaticResource DepthToMarginConverter}}">
        <!-- ... -->
    </StackPanel>
</DataTemplate>
```

**DepthToMarginConverter**:
```csharp
public class DepthToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int depth)
        {
            return new Thickness(depth * 20, 2, 2, 2); // 20pxずつインデント
        }
        return new Thickness(2);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

## 完了条件（チェックリスト）

- [ ] CheckItemTemplateがXAMLに定義されている
- [ ] CheckBoxのバインディングが正しく設定されている
- [ ] Buttonのバインディングが正しく設定されている
- [ ] CheckItemUIBuilderでDataTemplateを使用するように変更されている
- [ ] CreateCheckItemView()メソッドが実装されている
- [ ] DataTemplateが見つからない場合のフォールバックが実装されている
- [ ] 手動でUI生成するコードが削除されている（またはフォールバックに移動）
- [ ] ビルドが成功する
- [ ] 実行時にCheckBoxとButtonが正しく表示される
- [ ] バインディングが正しく動作する
- [ ] （オプション）スタイルが定義されている
- [ ] （オプション）DepthToMarginConverterが実装されている

## 技術メモ

### DataTemplateの配置先選択

**Window.Resources**（推奨）:
- ChecklistWindow専用のテンプレート
- 他のWindowに影響しない
- 変更がスコープ限定される

**App.xaml**:
- アプリケーション全体で共有
- MainWindowでも使用する場合に適している
- 一貫性が保たれる

### ContentPresenterの使用

```csharp
var contentPresenter = new ContentPresenter
{
    Content = viewModel, // DataContextとして設定
    ContentTemplate = dataTemplate // テンプレート適用
};
```

### FindResourceの注意点

```csharp
// リソースが見つからない場合はnullを返す
var dataTemplate = Application.Current.TryFindResource("CheckItemTemplate") as DataTemplate;

// リソースが見つからない場合は例外を発生
var dataTemplate = (DataTemplate)Application.Current.FindResource("CheckItemTemplate");
```

### バインディングのトラブルシューティング

XAMLのバインディングが動作しない場合：

1. Output Windowで「Binding」エラーを確認
2. DataContextが正しく設定されているか確認
3. プロパティ名が正しいか確認（大文字小文字区別）
4. INotifyPropertyChangedが実装されているか確認

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 5
- `src/DocumentFileManager.UI/Windows/ChecklistWindow.xaml` - DataTemplate配置先
- `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs` - DataTemplate使用側
