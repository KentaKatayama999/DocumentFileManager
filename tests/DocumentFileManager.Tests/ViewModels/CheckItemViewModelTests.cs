using System.IO;
using System.Windows;
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

    #region IsCheckBoxEnabled テスト（Phase 3拡張）

    /// <summary>
    /// MainWindowモードではチェックボックスは無効（読み取り専用）
    /// </summary>
    [Fact]
    public void IsCheckBoxEnabled_MainWindowモードでfalse()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };

        // Act
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: true);

        // Assert
        Assert.False(viewModel.IsCheckBoxEnabled);
    }

    /// <summary>
    /// ChecklistWindowモードではチェックボックスは有効
    /// </summary>
    [Fact]
    public void IsCheckBoxEnabled_ChecklistWindowモードでtrue()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };

        // Act
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: false);

        // Assert
        Assert.True(viewModel.IsCheckBoxEnabled);
    }

    #endregion

    #region CameraButtonVisibility テスト（Phase 3拡張）

    /// <summary>
    /// キャプチャがなければカメラボタンは非表示
    /// </summary>
    [Fact]
    public void CameraButtonVisibility_HasCapture_falseで非表示()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: false);
        viewModel.CaptureFilePath = null;

        // Act & Assert
        Assert.Equal(Visibility.Collapsed, viewModel.CameraButtonVisibility);
    }

    /// <summary>
    /// キャプチャファイルパスが設定されていてもファイルが存在しなければ非表示
    /// </summary>
    [Fact]
    public void CameraButtonVisibility_ファイル存在しない場合は非表示()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: false);
        viewModel.CaptureFilePath = "captures/nonexistent.png"; // 存在しないファイル

        // Act & Assert
        Assert.Equal(Visibility.Collapsed, viewModel.CameraButtonVisibility);
    }

    /// <summary>
    /// キャプチャが存在する場合はカメラボタンを表示
    /// </summary>
    [Fact]
    public void CameraButtonVisibility_HasCaptureとファイル存在でチェックON時に表示()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "CameraButtonVisibilityTest");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test_capture.png");
        File.WriteAllText(tempFile, "test");

        try
        {
            var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
            var viewModel = new CheckItemViewModel(entity, documentRootPath: tempDir, isMainWindow: false);
            viewModel.CaptureFilePath = "test_capture.png";
            viewModel.IsChecked = true; // ChecklistWindowモードではチェックON時のみ表示

            // Act & Assert
            Assert.Equal(Visibility.Visible, viewModel.CameraButtonVisibility);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// ChecklistWindowモードでチェックOFF時はキャプチャがあっても非表示
    /// </summary>
    [Fact]
    public void CameraButtonVisibility_チェックOFF時はキャプチャがあっても非表示()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "CameraButtonVisibilityTest2");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test_capture.png");
        File.WriteAllText(tempFile, "test");

        try
        {
            var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
            var viewModel = new CheckItemViewModel(entity, documentRootPath: tempDir, isMainWindow: false);
            viewModel.CaptureFilePath = "test_capture.png";
            viewModel.IsChecked = false; // チェックOFF

            // Act & Assert - チェックOFF時はキャプチャがあっても非表示（状態22）
            Assert.Equal(Visibility.Collapsed, viewModel.CameraButtonVisibility);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// MainWindowモードではチェック状態に関係なくキャプチャがあれば表示
    /// </summary>
    [Fact]
    public void CameraButtonVisibility_MainWindowモードではチェック状態に関係なく表示()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "CameraButtonVisibilityTest3");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test_capture.png");
        File.WriteAllText(tempFile, "test");

        try
        {
            var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
            var viewModel = new CheckItemViewModel(entity, documentRootPath: tempDir, isMainWindow: true);
            viewModel.CaptureFilePath = "test_capture.png";
            viewModel.IsChecked = false; // チェックOFF

            // Act & Assert - MainWindowモードではキャプチャがあれば常に表示
            Assert.Equal(Visibility.Visible, viewModel.CameraButtonVisibility);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// CaptureFilePathの変更でCameraButtonVisibilityのPropertyChangedが発火
    /// </summary>
    [Fact]
    public void CameraButtonVisibility_CaptureFilePath変更時にPropertyChanged発火()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: false);
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

        // Act
        viewModel.CaptureFilePath = "captures/screenshot.png";

        // Assert
        Assert.Contains(nameof(viewModel.CameraButtonVisibility), changedProperties);
    }

    #endregion

    #region DocumentRootPath テスト（Phase 3拡張）

    /// <summary>
    /// DocumentRootPathがコンストラクタで設定される
    /// </summary>
    [Fact]
    public void DocumentRootPath_コンストラクタで設定される()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var expectedPath = "C:\\TestDocuments";

        // Act
        var viewModel = new CheckItemViewModel(entity, documentRootPath: expectedPath, isMainWindow: false);

        // Assert
        Assert.Equal(expectedPath, viewModel.DocumentRootPath);
    }

    #endregion

    #region GetCaptureAbsolutePath テスト（Phase 3拡張）

    /// <summary>
    /// キャプチャの絶対パスを取得
    /// </summary>
    [Fact]
    public void GetCaptureAbsolutePath_相対パスから絶対パスを生成()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: false);
        viewModel.CaptureFilePath = "captures\\screenshot.png";

        // Act
        var absolutePath = viewModel.GetCaptureAbsolutePath();

        // Assert
        Assert.Equal("C:\\TestDocuments\\captures\\screenshot.png", absolutePath);
    }

    /// <summary>
    /// CaptureFilePathがnullの場合はnullを返す
    /// </summary>
    [Fact]
    public void GetCaptureAbsolutePath_CaptureFilePathがnull_nullを返す()
    {
        // Arrange
        var entity = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var viewModel = new CheckItemViewModel(entity, documentRootPath: "C:\\TestDocuments", isMainWindow: false);
        viewModel.CaptureFilePath = null;

        // Act
        var absolutePath = viewModel.GetCaptureAbsolutePath();

        // Assert
        Assert.Null(absolutePath);
    }

    #endregion
}
