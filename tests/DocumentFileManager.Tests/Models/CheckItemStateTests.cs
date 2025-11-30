using System.Windows;
using DocumentFileManager.UI.Models;
using Xunit;

namespace DocumentFileManager.Tests.Models;

/// <summary>
/// CheckItemStateクラスの単体テスト
/// </summary>
public class CheckItemStateTests
{
    #region コンストラクタテスト

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var state = new CheckItemState(WindowMode.MainWindow, "10", true);

        // Assert
        Assert.Equal(WindowMode.MainWindow, state.WindowMode);
        Assert.Equal("10", state.ItemState);
        Assert.True(state.CaptureFileExists);
    }

    [Fact]
    public void Constructor_WithChecklistWindow_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var state = new CheckItemState(WindowMode.ChecklistWindow, "11", false);

        // Assert
        Assert.Equal(WindowMode.ChecklistWindow, state.WindowMode);
        Assert.Equal("11", state.ItemState);
        Assert.False(state.CaptureFileExists);
    }

    #endregion

    #region IsCheckBoxEnabled テスト

    [Fact]
    public void IsCheckBoxEnabled_MainWindow_ReturnsFalse()
    {
        // Arrange
        var state = new CheckItemState(WindowMode.MainWindow, "00", false);

        // Act & Assert
        Assert.False(state.IsCheckBoxEnabled);
    }

    [Fact]
    public void IsCheckBoxEnabled_ChecklistWindow_ReturnsTrue()
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, "00", false);

        // Act & Assert
        Assert.True(state.IsCheckBoxEnabled);
    }

    #endregion

    #region CameraButtonVisibility - MainWindow テスト

    [Theory]
    [InlineData("00", false, Visibility.Collapsed)]
    [InlineData("00", true, Visibility.Visible)]
    [InlineData("10", false, Visibility.Collapsed)]
    [InlineData("10", true, Visibility.Visible)]
    [InlineData("11", false, Visibility.Collapsed)]
    [InlineData("11", true, Visibility.Visible)]
    [InlineData("20", false, Visibility.Collapsed)]
    [InlineData("20", true, Visibility.Visible)]
    [InlineData("22", false, Visibility.Collapsed)]
    [InlineData("22", true, Visibility.Visible)]
    public void CameraButtonVisibility_MainWindow_DependsOnCaptureFileExists(
        string itemState, bool captureFileExists, Visibility expected)
    {
        // Arrange
        var state = new CheckItemState(WindowMode.MainWindow, itemState, captureFileExists);

        // Act & Assert
        Assert.Equal(expected, state.CameraButtonVisibility);
    }

    #endregion

    #region CameraButtonVisibility - ChecklistWindow テスト

    [Theory]
    [InlineData("00", false, Visibility.Collapsed)] // 未紐づけ、キャプチャなし
    [InlineData("00", true, Visibility.Collapsed)]  // 未紐づけ、キャプチャあり（ItemState[1]=='0'）
    [InlineData("10", false, Visibility.Collapsed)] // チェックON、キャプチャなし
    [InlineData("10", true, Visibility.Collapsed)]  // チェックON、キャプチャあり（ItemState[1]=='0'）
    [InlineData("11", false, Visibility.Collapsed)] // チェックON、キャプチャあり（ItemState[1]=='1'）だがファイルなし
    [InlineData("11", true, Visibility.Visible)]    // チェックON、キャプチャあり（ItemState[1]=='1'）★Visible
    [InlineData("20", false, Visibility.Collapsed)] // チェックOFF（履歴あり）、キャプチャなし
    [InlineData("20", true, Visibility.Collapsed)]  // チェックOFF（履歴あり）、キャプチャあり（ItemState[1]=='0'）
    [InlineData("22", false, Visibility.Collapsed)] // チェックOFF（履歴あり）、キャプチャあり（ItemState[1]=='2'）だがファイルなし
    [InlineData("22", true, Visibility.Collapsed)]  // チェックOFF（履歴あり）、キャプチャあり（ItemState[1]=='2'）
    public void CameraButtonVisibility_ChecklistWindow_DependsOnItemStateAndCaptureFileExists(
        string itemState, bool captureFileExists, Visibility expected)
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, itemState, captureFileExists);

        // Act & Assert
        Assert.Equal(expected, state.CameraButtonVisibility);
    }

    #endregion

    #region ItemState 更新テスト

    [Fact]
    public void ItemState_CanBeUpdated()
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, "00", false);

        // Act
        state.ItemState = "10";

        // Assert
        Assert.Equal("10", state.ItemState);
    }

    [Fact]
    public void CaptureFileExists_CanBeUpdated()
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, "10", false);

        // Act
        state.CaptureFileExists = true;

        // Assert
        Assert.True(state.CaptureFileExists);
    }

    [Fact]
    public void CameraButtonVisibility_UpdatesWhenItemStateChanges()
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, "10", true);
        Assert.Equal(Visibility.Collapsed, state.CameraButtonVisibility); // 初期状態

        // Act
        state.ItemState = "11";

        // Assert
        Assert.Equal(Visibility.Visible, state.CameraButtonVisibility);
    }

    [Fact]
    public void CameraButtonVisibility_UpdatesWhenCaptureFileExistsChanges()
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, "11", false);
        Assert.Equal(Visibility.Collapsed, state.CameraButtonVisibility); // 初期状態

        // Act
        state.CaptureFileExists = true;

        // Assert
        Assert.Equal(Visibility.Visible, state.CameraButtonVisibility);
    }

    #endregion

    #region エッジケーステスト

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("000")]
    public void CameraButtonVisibility_InvalidItemState_ReturnsCollapsed(string? itemState)
    {
        // Arrange
        var state = new CheckItemState(WindowMode.ChecklistWindow, itemState ?? "", true);

        // Act & Assert
        Assert.Equal(Visibility.Collapsed, state.CameraButtonVisibility);
    }

    [Fact]
    public void WindowMode_Enum_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)WindowMode.MainWindow);
        Assert.Equal(1, (int)WindowMode.ChecklistWindow);
    }

    #endregion
}
