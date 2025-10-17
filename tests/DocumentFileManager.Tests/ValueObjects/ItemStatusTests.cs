using DocumentFileManager.ValueObjects;
using Xunit;

namespace DocumentFileManager.Tests.ValueObjects;

/// <summary>
/// ItemStatus列挙型の単体テスト
/// </summary>
public class ItemStatusTests
{
    [Fact]
    public void Unspecified_値が0()
    {
        // Act & Assert
        Assert.Equal(0, (int)ItemStatus.Unspecified);
    }

    [Fact]
    public void Current_値が1()
    {
        // Act & Assert
        Assert.Equal(1, (int)ItemStatus.Current);
    }

    [Fact]
    public void Revised_値が2()
    {
        // Act & Assert
        Assert.Equal(2, (int)ItemStatus.Revised);
    }

    [Fact]
    public void Cancelled_値が3()
    {
        // Act & Assert
        Assert.Equal(3, (int)ItemStatus.Cancelled);
    }

    [Fact]
    public void ToString_正しい名前を返す()
    {
        // Act & Assert
        Assert.Equal("Unspecified", ItemStatus.Unspecified.ToString());
        Assert.Equal("Current", ItemStatus.Current.ToString());
        Assert.Equal("Revised", ItemStatus.Revised.ToString());
        Assert.Equal("Cancelled", ItemStatus.Cancelled.ToString());
    }

    [Fact]
    public void 数値からキャスト可能()
    {
        // Act
        var status0 = (ItemStatus)0;
        var status1 = (ItemStatus)1;
        var status2 = (ItemStatus)2;
        var status3 = (ItemStatus)3;

        // Assert
        Assert.Equal(ItemStatus.Unspecified, status0);
        Assert.Equal(ItemStatus.Current, status1);
        Assert.Equal(ItemStatus.Revised, status2);
        Assert.Equal(ItemStatus.Cancelled, status3);
    }

    [Fact]
    public void デフォルト値はUnspecified()
    {
        // Arrange
        ItemStatus status = default;

        // Act & Assert
        Assert.Equal(ItemStatus.Unspecified, status);
    }
}
