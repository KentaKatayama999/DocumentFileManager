using DocumentFileManager.Entities;
using DocumentFileManager.UI.Factories;
using DocumentFileManager.UI.Models;
using DocumentFileManager.ValueObjects;
using Xunit;

namespace DocumentFileManager.Tests.Factories;

/// <summary>
/// CheckItemViewModelFactoryの単体テスト
/// </summary>
public class CheckItemViewModelFactoryTests
{
    private readonly string _testDocumentRootPath = "C:\\TestDocuments";

    #region Create テスト

    [Fact]
    public void Create_BasicEntity_ReturnsViewModel()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem
        {
            Id = 1,
            Path = "設計図面/平面図",
            Label = "平面図",
            Status = ItemStatus.Unspecified
        };

        // Act
        var viewModel = factory.Create(entity, WindowMode.MainWindow);

        // Assert
        Assert.NotNull(viewModel);
        Assert.Equal(1, viewModel.Id);
        Assert.Equal("平面図", viewModel.Label);
        Assert.True(viewModel.IsMainWindow);
        Assert.NotNull(viewModel.State);
        Assert.Equal(WindowMode.MainWindow, viewModel.State.WindowMode);
    }

    [Fact]
    public void Create_WithChecklistWindow_SetsCorrectWindowMode()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem { Id = 1, Path = "test", Label = "Test" };

        // Act
        var viewModel = factory.Create(entity, WindowMode.ChecklistWindow);

        // Assert
        Assert.False(viewModel.IsMainWindow);
        Assert.Equal(WindowMode.ChecklistWindow, viewModel.State.WindowMode);
    }

    [Fact]
    public void Create_WithCheckItemDocument_SetsCheckedState()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem { Id = 1, Path = "test", Label = "Test" };
        var checkItemDocument = new CheckItemDocument
        {
            CheckItemId = 1,
            DocumentId = 1,
            IsChecked = true,
            CaptureFile = null
        };

        // Act
        var viewModel = factory.Create(entity, WindowMode.ChecklistWindow, checkItemDocument);

        // Assert
        Assert.True(viewModel.IsChecked);
        Assert.Equal("10", viewModel.State.ItemState); // チェックON、キャプチャなし
    }

    [Fact]
    public void Create_WithCaptureFile_SetsCaptureFilePath()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem { Id = 1, Path = "test", Label = "Test" };
        var checkItemDocument = new CheckItemDocument
        {
            CheckItemId = 1,
            DocumentId = 1,
            IsChecked = true,
            CaptureFile = "captures/screenshot.png"
        };

        // Act
        var viewModel = factory.Create(entity, WindowMode.ChecklistWindow, checkItemDocument);

        // Assert
        Assert.Equal("captures/screenshot.png", viewModel.CaptureFilePath);
    }

    [Fact]
    public void Create_MainWindowWithCheckItemDocument_DoesNotSetIsChecked()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem { Id = 1, Path = "test", Label = "Test", Status = ItemStatus.Unspecified };
        var checkItemDocument = new CheckItemDocument
        {
            CheckItemId = 1,
            DocumentId = 1,
            IsChecked = true, // これはMainWindowでは無視される
            CaptureFile = "captures/test.png"
        };

        // Act
        var viewModel = factory.Create(entity, WindowMode.MainWindow, checkItemDocument);

        // Assert
        Assert.False(viewModel.IsChecked); // MainWindowではIsCheckedはEntityのStatusから決定
        Assert.Equal("captures/test.png", viewModel.CaptureFilePath);
    }

    [Fact]
    public void Create_NoCheckItemDocument_SetsItemState00()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem { Id = 1, Path = "test", Label = "Test" };

        // Act
        var viewModel = factory.Create(entity, WindowMode.ChecklistWindow);

        // Assert
        Assert.Equal("00", viewModel.State.ItemState); // 未紐づけ
    }

    #endregion

    #region CreateHierarchy テスト

    [Fact]
    public void CreateHierarchy_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entities = new List<CheckItem>();

        // Act
        var result = factory.CreateHierarchy(entities, WindowMode.MainWindow);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CreateHierarchy_SingleEntity_ReturnsSingleViewModel()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entities = new List<CheckItem>
        {
            new CheckItem { Id = 1, Path = "test", Label = "Test" }
        };

        // Act
        var result = factory.CreateHierarchy(entities, WindowMode.MainWindow);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test", result[0].Label);
    }

    [Fact]
    public void CreateHierarchy_WithChildren_CreatesNestedStructure()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var childEntity = new CheckItem { Id = 2, Path = "parent/child", Label = "Child" };
        var parentEntity = new CheckItem
        {
            Id = 1,
            Path = "parent",
            Label = "Parent",
            Children = new List<CheckItem> { childEntity }
        };
        var entities = new List<CheckItem> { parentEntity };

        // Act
        var result = factory.CreateHierarchy(entities, WindowMode.MainWindow);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Children);
        Assert.Equal("Child", result[0].Children[0].Label);
    }

    [Fact]
    public void CreateHierarchy_WithCheckItemDocuments_AppliesStateCorrectly()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entities = new List<CheckItem>
        {
            new CheckItem { Id = 1, Path = "test1", Label = "Test1" },
            new CheckItem { Id = 2, Path = "test2", Label = "Test2" }
        };
        var checkItemDocuments = new Dictionary<int, CheckItemDocument>
        {
            { 1, new CheckItemDocument { CheckItemId = 1, DocumentId = 1, IsChecked = true } },
            { 2, new CheckItemDocument { CheckItemId = 2, DocumentId = 1, IsChecked = false } }
        };

        // Act
        var result = factory.CreateHierarchy(entities, WindowMode.ChecklistWindow, checkItemDocuments);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsChecked);
        Assert.False(result[1].IsChecked);
    }

    [Fact]
    public void CreateHierarchy_DeepNesting_CreatesCorrectStructure()
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var grandchildEntity = new CheckItem { Id = 3, Path = "a/b/c", Label = "Grandchild" };
        var childEntity = new CheckItem
        {
            Id = 2,
            Path = "a/b",
            Label = "Child",
            Children = new List<CheckItem> { grandchildEntity }
        };
        var parentEntity = new CheckItem
        {
            Id = 1,
            Path = "a",
            Label = "Parent",
            Children = new List<CheckItem> { childEntity }
        };
        var entities = new List<CheckItem> { parentEntity };

        // Act
        var result = factory.CreateHierarchy(entities, WindowMode.MainWindow);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Children);
        Assert.Single(result[0].Children[0].Children);
        Assert.Equal("Grandchild", result[0].Children[0].Children[0].Label);
    }

    #endregion

    #region ItemState 決定ロジック テスト

    [Theory]
    [InlineData(true, false, "10")]  // チェックON、キャプチャなし
    [InlineData(false, false, "20")] // チェックOFF（履歴あり）、キャプチャなし
    public void Create_ItemStateLogic_CalculatesCorrectly(
        bool isChecked, bool hasCaptureFile, string expectedItemState)
    {
        // Arrange
        var factory = new CheckItemViewModelFactory(_testDocumentRootPath);
        var entity = new CheckItem { Id = 1, Path = "test", Label = "Test" };
        var checkItemDocument = new CheckItemDocument
        {
            CheckItemId = 1,
            DocumentId = 1,
            IsChecked = isChecked,
            CaptureFile = hasCaptureFile ? "captures/test.png" : null
        };

        // Act
        var viewModel = factory.Create(entity, WindowMode.ChecklistWindow, checkItemDocument);

        // Assert
        // ファイルが存在しない場合、CaptureFileExistsはfalseなので、11/22にはならない
        Assert.Equal(expectedItemState, viewModel.State.ItemState);
    }

    #endregion
}
