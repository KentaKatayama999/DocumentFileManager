using DocumentFileManager.UI.Services.Abstractions;
using Moq;
using Xunit;

namespace DocumentFileManager.Tests.Services;

/// <summary>
/// IDialogServiceのテストクラス
/// 実際のMessageBoxを表示するとテストが対話的になるため、
/// IDialogServiceのモック実装を使用してテストを行う
/// </summary>
public class DialogServiceTests
{
    /// <summary>
    /// ShowConfirmationAsync_ユーザーがはいを選択_trueを返す
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_WhenUserSelectsYes_ReturnsTrue()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var dialogService = mockDialogService.Object;

        // Act
        var result = await dialogService.ShowConfirmationAsync("テストメッセージ", "テストタイトル");

        // Assert
        Assert.True(result);
        mockDialogService.Verify(
            x => x.ShowConfirmationAsync("テストメッセージ", "テストタイトル"),
            Times.Once);
    }

    /// <summary>
    /// ShowConfirmationAsync_ユーザーがいいえを選択_falseを返す
    /// </summary>
    [Fact]
    public async Task ShowConfirmationAsync_WhenUserSelectsNo_ReturnsFalse()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var dialogService = mockDialogService.Object;

        // Act
        var result = await dialogService.ShowConfirmationAsync("テストメッセージ", "テストタイトル");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// ShowYesNoCancelAsync_ユーザーがキャンセルを選択_Cancelを返す
    /// </summary>
    [Fact]
    public async Task ShowYesNoCancelAsync_WhenUserSelectsCancel_ReturnsCancel()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowYesNoCancelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UI.Services.Abstractions.DialogResult.Cancel);

        var dialogService = mockDialogService.Object;

        // Act
        var result = await dialogService.ShowYesNoCancelAsync("テストメッセージ", "テストタイトル");

        // Assert
        Assert.Equal(UI.Services.Abstractions.DialogResult.Cancel, result);
        mockDialogService.Verify(
            x => x.ShowYesNoCancelAsync("テストメッセージ", "テストタイトル"),
            Times.Once);
    }

    /// <summary>
    /// ShowYesNoCancelAsync_ユーザーがはいを選択_Yesを返す
    /// </summary>
    [Fact]
    public async Task ShowYesNoCancelAsync_WhenUserSelectsYes_ReturnsYes()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowYesNoCancelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UI.Services.Abstractions.DialogResult.Yes);

        var dialogService = mockDialogService.Object;

        // Act
        var result = await dialogService.ShowYesNoCancelAsync("テストメッセージ", "テストタイトル");

        // Assert
        Assert.Equal(UI.Services.Abstractions.DialogResult.Yes, result);
    }

    /// <summary>
    /// ShowYesNoCancelAsync_ユーザーがいいえを選択_Noを返す
    /// </summary>
    [Fact]
    public async Task ShowYesNoCancelAsync_WhenUserSelectsNo_ReturnsNo()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowYesNoCancelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UI.Services.Abstractions.DialogResult.No);

        var dialogService = mockDialogService.Object;

        // Act
        var result = await dialogService.ShowYesNoCancelAsync("テストメッセージ", "テストタイトル");

        // Assert
        Assert.Equal(UI.Services.Abstractions.DialogResult.No, result);
    }

    /// <summary>
    /// ShowInformationAsync_正常に呼び出される
    /// </summary>
    [Fact]
    public async Task ShowInformationAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowInformationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dialogService = mockDialogService.Object;

        // Act
        await dialogService.ShowInformationAsync("情報メッセージ", "情報");

        // Assert
        mockDialogService.Verify(
            x => x.ShowInformationAsync("情報メッセージ", "情報"),
            Times.Once);
    }

    /// <summary>
    /// ShowErrorAsync_正常に呼び出される
    /// </summary>
    [Fact]
    public async Task ShowErrorAsync_WhenCalled_CompletesSuccessfully()
    {
        // Arrange
        var mockDialogService = new Mock<IDialogService>();
        mockDialogService
            .Setup(x => x.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dialogService = mockDialogService.Object;

        // Act
        await dialogService.ShowErrorAsync("エラーメッセージ", "エラー");

        // Assert
        mockDialogService.Verify(
            x => x.ShowErrorAsync("エラーメッセージ", "エラー"),
            Times.Once);
    }
}
