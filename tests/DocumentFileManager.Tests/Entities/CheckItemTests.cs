using DocumentFileManager.Entities;
using DocumentFileManager.ValueObjects;
using Xunit;

namespace DocumentFileManager.Tests.Entities;

/// <summary>
/// CheckItemエンティティの単体テスト
/// </summary>
public class CheckItemTests
{
    [Fact]
    public void GeneratePath_ルート項目_ラベルのみ返す()
    {
        // Arrange
        var rootItem = new CheckItem
        {
            Id = 1,
            Label = "設計図面",
            Parent = null
        };

        // Act
        var path = rootItem.GeneratePath();

        // Assert
        Assert.Equal("設計図面", path);
    }

    [Fact]
    public void GeneratePath_子項目_階層パスを返す()
    {
        // Arrange
        var rootItem = new CheckItem
        {
            Id = 1,
            Label = "設計図面",
            Parent = null
        };

        var childItem = new CheckItem
        {
            Id = 2,
            Label = "平面図",
            Parent = rootItem
        };

        // Act
        var path = childItem.GeneratePath();

        // Assert
        Assert.Equal("設計図面/平面図", path);
    }

    [Fact]
    public void GeneratePath_孫項目_完全な階層パスを返す()
    {
        // Arrange
        var rootItem = new CheckItem
        {
            Id = 1,
            Label = "設計図面",
            Parent = null
        };

        var childItem = new CheckItem
        {
            Id = 2,
            Label = "平面図",
            Parent = rootItem
        };

        var grandchildItem = new CheckItem
        {
            Id = 3,
            Label = "1階",
            Parent = childItem
        };

        // Act
        var path = grandchildItem.GeneratePath();

        // Assert
        Assert.Equal("設計図面/平面図/1階", path);
    }

    [Fact]
    public void AdvanceStatus_未指定から現行へ()
    {
        // Arrange
        var item = new CheckItem
        {
            Status = ItemStatus.Unspecified
        };

        // Act
        item.AdvanceStatus();

        // Assert
        Assert.Equal(ItemStatus.Current, item.Status);
    }

    [Fact]
    public void AdvanceStatus_現行から改訂へ()
    {
        // Arrange
        var item = new CheckItem
        {
            Status = ItemStatus.Current
        };

        // Act
        item.AdvanceStatus();

        // Assert
        Assert.Equal(ItemStatus.Revised, item.Status);
    }

    [Fact]
    public void AdvanceStatus_改訂からキャンセルへ()
    {
        // Arrange
        var item = new CheckItem
        {
            Status = ItemStatus.Revised
        };

        // Act
        item.AdvanceStatus();

        // Assert
        Assert.Equal(ItemStatus.Cancelled, item.Status);
    }

    [Fact]
    public void AdvanceStatus_キャンセルから未指定へ()
    {
        // Arrange
        var item = new CheckItem
        {
            Status = ItemStatus.Cancelled
        };

        // Act
        item.AdvanceStatus();

        // Assert
        Assert.Equal(ItemStatus.Unspecified, item.Status);
    }

    [Fact]
    public void Children_初期状態は空のコレクション()
    {
        // Arrange & Act
        var item = new CheckItem();

        // Assert
        Assert.Empty(item.Children);
    }

    [Fact]
    public void Children_子要素を追加できる()
    {
        // Arrange
        var parent = new CheckItem
        {
            Id = 1,
            Label = "親項目"
        };

        var child = new CheckItem
        {
            Id = 2,
            Label = "子項目",
            Parent = parent
        };

        // Act
        parent.Children.Add(child);

        // Assert
        Assert.Single(parent.Children);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void デフォルト値の確認()
    {
        // Arrange & Act
        var item = new CheckItem();

        // Assert
        Assert.Equal(0, item.Id);
        Assert.Equal(string.Empty, item.Path);
        Assert.Equal(string.Empty, item.Label);
        Assert.Equal(ItemStatus.Unspecified, item.Status);
        Assert.Null(item.ParentId);
        Assert.Null(item.Parent);
        Assert.Empty(item.Children);
        Assert.Empty(item.LinkedDocuments);
    }
}
