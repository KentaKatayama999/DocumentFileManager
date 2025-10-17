using DocumentFileManager.Entities;
using DocumentFileManager.UI.ViewModels;
using DocumentFileManager.ValueObjects;
using Xunit;

namespace DocumentFileManager.Tests.ViewModels;

/// <summary>
/// CheckItemViewModelの単体テスト
/// </summary>
public class CheckItemViewModelTests
{
    [Fact]
    public void Constructor_エンティティからViewModelを作成()
    {
        // Arrange
        var entity = new CheckItem
        {
            Id = 1,
            Path = "設計図面/平面図",
            Label = "平面図",
            Status = ItemStatus.Current
        };

        // Act
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.Equal(1, viewModel.Id);
        Assert.Equal("設計図面/平面図", viewModel.Path);
        Assert.Equal("平面図", viewModel.Label);
        Assert.Equal(ItemStatus.Current, viewModel.Status);
        Assert.True(viewModel.IsChecked); // Current状態なのでチェック済み
    }

    [Fact]
    public void Constructor_Unspecified状態_IsCheckedがFalse()
    {
        // Arrange
        var entity = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Unspecified
        };

        // Act
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.False(viewModel.IsChecked);
        Assert.Equal(ItemStatus.Unspecified, viewModel.Status);
    }

    [Fact]
    public void IsChecked_変更時にPropertyChangedが発火()
    {
        // Arrange
        var entity = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Unspecified
        };
        var viewModel = new CheckItemViewModel(entity);
        var propertyChangedFired = false;
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedFired = true;
            changedProperties.Add(e.PropertyName);
        };

        // Act
        viewModel.IsChecked = true;

        // Assert
        Assert.True(propertyChangedFired);
        Assert.Contains(nameof(viewModel.IsChecked), changedProperties);
    }

    [Fact]
    public void IsChecked_Trueに変更_StatusがCurrentになる()
    {
        // Arrange
        var entity = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Unspecified
        };
        var viewModel = new CheckItemViewModel(entity);

        // Act
        viewModel.IsChecked = true;

        // Assert
        Assert.True(viewModel.IsChecked);
        Assert.Equal(ItemStatus.Current, viewModel.Status);
        Assert.Equal(ItemStatus.Current, entity.Status); // エンティティも更新される
    }

    [Fact]
    public void IsChecked_Falseに変更_StatusがUnspecifiedになる()
    {
        // Arrange
        var entity = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Current
        };
        var viewModel = new CheckItemViewModel(entity);

        // Act
        viewModel.IsChecked = false;

        // Assert
        Assert.False(viewModel.IsChecked);
        Assert.Equal(ItemStatus.Unspecified, viewModel.Status);
        Assert.Equal(ItemStatus.Unspecified, entity.Status);
    }

    [Fact]
    public void IsChecked_同じ値に設定_PropertyChangedが発火しない()
    {
        // Arrange
        var entity = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Current
        };
        var viewModel = new CheckItemViewModel(entity);
        var propertyChangedCount = 0;

        viewModel.PropertyChanged += (sender, e) => propertyChangedCount++;

        // Act
        viewModel.IsChecked = true; // 既にtrue

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    [Fact]
    public void IsChecked_変更時にStatusのPropertyChangedも発火()
    {
        // Arrange
        var entity = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Unspecified
        };
        var viewModel = new CheckItemViewModel(entity);
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

        // Act
        viewModel.IsChecked = true;

        // Assert
        Assert.Contains(nameof(viewModel.IsChecked), changedProperties);
        Assert.Contains(nameof(viewModel.Status), changedProperties);
    }

    [Fact]
    public void CaptureFilePath_変更時にPropertyChangedが発火()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        var propertyChangedFired = false;
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedFired = true;
            changedProperties.Add(e.PropertyName);
        };

        // Act
        viewModel.CaptureFilePath = "captures/screenshot.png";

        // Assert
        Assert.True(propertyChangedFired);
        Assert.Contains(nameof(viewModel.CaptureFilePath), changedProperties);
    }

    [Fact]
    public void CaptureFilePath_変更時にHasCaptureのPropertyChangedも発火()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

        // Act
        viewModel.CaptureFilePath = "captures/screenshot.png";

        // Assert
        Assert.Contains(nameof(viewModel.CaptureFilePath), changedProperties);
        Assert.Contains(nameof(viewModel.HasCapture), changedProperties);
    }

    [Fact]
    public void CaptureFilePath_同じ値に設定_PropertyChangedが発火しない()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        viewModel.CaptureFilePath = "captures/screenshot.png";
        var propertyChangedCount = 0;

        viewModel.PropertyChanged += (sender, e) => propertyChangedCount++;

        // Act
        viewModel.CaptureFilePath = "captures/screenshot.png";

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    [Fact]
    public void HasCapture_CaptureFilePathがNull_Falseを返す()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.False(viewModel.HasCapture);
    }

    [Fact]
    public void HasCapture_CaptureFilePathが空文字_Falseを返す()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        viewModel.CaptureFilePath = "";

        // Assert
        Assert.False(viewModel.HasCapture);
    }

    [Fact]
    public void HasCapture_CaptureFilePathが設定されている_Trueを返す()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        viewModel.CaptureFilePath = "captures/screenshot.png";

        // Assert
        Assert.True(viewModel.HasCapture);
    }

    [Fact]
    public void HasCapture_CaptureFilePathをNullに変更_Falseになる()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        viewModel.CaptureFilePath = "captures/screenshot.png";
        Assert.True(viewModel.HasCapture);

        // Act
        viewModel.CaptureFilePath = null;

        // Assert
        Assert.False(viewModel.HasCapture);
    }

    [Fact]
    public void Children_初期状態は空のコレクション()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };

        // Act
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.Empty(viewModel.Children);
    }

    [Fact]
    public void Children_子要素を追加できる()
    {
        // Arrange
        var parentEntity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var childEntity = new CheckItem { Path = "設計図面/平面図", Label = "平面図" };
        var parentViewModel = new CheckItemViewModel(parentEntity);
        var childViewModel = new CheckItemViewModel(childEntity);

        // Act
        parentViewModel.Children.Add(childViewModel);

        // Assert
        Assert.Single(parentViewModel.Children);
        Assert.Contains(childViewModel, parentViewModel.Children);
    }

    [Fact]
    public void IsCategory_子要素あり_Trueを返す()
    {
        // Arrange
        var parentEntity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var childEntity = new CheckItem { Path = "設計図面/平面図", Label = "平面図" };
        var parentViewModel = new CheckItemViewModel(parentEntity);
        var childViewModel = new CheckItemViewModel(childEntity);
        parentViewModel.Children.Add(childViewModel);

        // Assert
        Assert.True(parentViewModel.IsCategory);
    }

    [Fact]
    public void IsCategory_子要素なし_Falseを返す()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.False(viewModel.IsCategory);
    }

    [Fact]
    public void IsItem_子要素なし_Trueを返す()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.True(viewModel.IsItem);
    }

    [Fact]
    public void IsItem_子要素あり_Falseを返す()
    {
        // Arrange
        var parentEntity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var childEntity = new CheckItem { Path = "設計図面/平面図", Label = "平面図" };
        var parentViewModel = new CheckItemViewModel(parentEntity);
        var childViewModel = new CheckItemViewModel(childEntity);
        parentViewModel.Children.Add(childViewModel);

        // Assert
        Assert.False(parentViewModel.IsItem);
    }

    [Fact]
    public void Entity_コンストラクタで渡したエンティティを保持()
    {
        // Arrange
        var entity = new CheckItem
        {
            Id = 99,
            Path = "設計図面",
            Label = "設計図面"
        };

        // Act
        var viewModel = new CheckItemViewModel(entity);

        // Assert
        Assert.Same(entity, viewModel.Entity);
    }

    [Fact]
    public void PropertyChanged_複数のリスナーを登録できる()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity);
        var listener1Fired = false;
        var listener2Fired = false;

        viewModel.PropertyChanged += (sender, e) => listener1Fired = true;
        viewModel.PropertyChanged += (sender, e) => listener2Fired = true;

        // Act
        viewModel.IsChecked = true;

        // Assert
        Assert.True(listener1Fired);
        Assert.True(listener2Fired);
    }
}
