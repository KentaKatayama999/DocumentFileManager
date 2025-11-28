using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Models;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.Services.Abstractions;
using DocumentFileManager.UI.ViewModels;
using DocumentFileManager.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocumentFileManager.Tests.Services;

/// <summary>
/// ChecklistStateManagerのテストクラス
/// TDDアプローチ: テストを先に作成し、実装は後から行う
/// </summary>
public class ChecklistStateManagerTests
{
    private readonly Mock<ICheckItemDocumentRepository> _mockRepository;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ILogger<ChecklistStateManager>> _mockLogger;

    public ChecklistStateManagerTests()
    {
        _mockRepository = new Mock<ICheckItemDocumentRepository>();
        _mockDialogService = new Mock<IDialogService>();
        _mockLogger = new Mock<ILogger<ChecklistStateManager>>();
    }

    #region ヘルパーメソッド

    /// <summary>
    /// テスト用のCheckItemViewModelを作成
    /// </summary>
    private CheckItemViewModel CreateTestViewModel(
        int id = 1,
        bool isChecked = false,
        string? captureFilePath = null)
    {
        var entity = new CheckItem
        {
            Id = id,
            Label = "テスト項目",
            Path = "テスト/テスト項目",
            Status = isChecked ? ItemStatus.Current : ItemStatus.Unspecified
        };

        var viewModel = new CheckItemViewModel(entity)
        {
            CaptureFilePath = captureFilePath
        };

        // IsCheckedは内部で設定されるのでEntityのStatusを使用
        if (isChecked)
        {
            viewModel.IsChecked = true;
        }

        return viewModel;
    }

    /// <summary>
    /// テスト用のDocumentを作成
    /// </summary>
    private Document CreateTestDocument(int id = 1)
    {
        return new Document
        {
            Id = id,
            FileName = "テストドキュメント.pdf",
            RelativePath = "documents/テストドキュメント.pdf",
            FileType = ".pdf"
        };
    }

    /// <summary>
    /// テスト用のCheckItemDocumentを作成
    /// </summary>
    private CheckItemDocument CreateTestCheckItemDocument(
        int id = 1,
        int checkItemId = 1,
        int documentId = 1,
        bool isChecked = true,
        string? captureFile = null)
    {
        return new CheckItemDocument
        {
            Id = id,
            CheckItemId = checkItemId,
            DocumentId = documentId,
            IsChecked = isChecked,
            CaptureFile = captureFile,
            LinkedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// ChecklistStateManagerを作成
    /// </summary>
    private ChecklistStateManager CreateStateManager()
    {
        return new ChecklistStateManager(
            _mockRepository.Object,
            _mockDialogService.Object,
            _mockLogger.Object);
    }

    #endregion

    #region チェックON時のテスト

    /// <summary>
    /// 状態00（未紐づけ）でチェックON、キャプチャ確認で「いいえ」→状態10へ遷移
    /// </summary>
    [Fact]
    public async Task HandleCheckOnAsync_未紐づけ_キャプチャなし_状態10へ遷移()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: false);
        var document = CreateTestDocument();

        // 既存レコードなし
        _mockRepository
            .Setup(x => x.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id))
            .ReturnsAsync((CheckItemDocument?)null);

        // キャプチャ確認で「いいえ」を選択
        _mockDialogService
            .Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var transition = await stateManager.HandleCheckOnAsync(viewModel, document);

        // Assert
        Assert.NotNull(transition);
        Assert.True(transition.IsChecked);
        Assert.Null(transition.CaptureFile);
        Assert.Equal("10", transition.TargetState);
        Assert.True(transition.HasChanges);
    }

    /// <summary>
    /// 状態22（キャプチャあり、チェックOFF）でチェックON、復帰確認で「はい」→状態11へ遷移
    /// </summary>
    [Fact]
    public async Task HandleCheckOnAsync_既存キャプチャあり_復帰選択_状態11へ遷移()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: false, captureFilePath: "captures/test.png");
        var document = CreateTestDocument();

        // 既存レコードあり（状態22: チェックOFF、キャプチャあり）
        var existingRecord = CreateTestCheckItemDocument(
            isChecked: false,
            captureFile: "captures/test.png");

        _mockRepository
            .Setup(x => x.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id))
            .ReturnsAsync(existingRecord);

        // 復帰確認で「はい」を選択
        _mockDialogService
            .Setup(x => x.ShowYesNoCancelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UI.Services.Abstractions.DialogResult.Yes);

        // Act
        var transition = await stateManager.HandleCheckOnAsync(viewModel, document);

        // Assert
        Assert.NotNull(transition);
        Assert.True(transition.IsChecked);
        Assert.Equal("captures/test.png", transition.CaptureFile);
        Assert.Equal("11", transition.TargetState);
    }

    /// <summary>
    /// 状態22（キャプチャあり、チェックOFF）でチェックON、復帰確認で「いいえ」→状態10へ遷移
    /// </summary>
    [Fact]
    public async Task HandleCheckOnAsync_既存キャプチャあり_破棄選択_状態10へ遷移()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: false, captureFilePath: "captures/test.png");
        var document = CreateTestDocument();

        // 既存レコードあり（状態22: チェックOFF、キャプチャあり）
        var existingRecord = CreateTestCheckItemDocument(
            isChecked: false,
            captureFile: "captures/test.png");

        _mockRepository
            .Setup(x => x.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id))
            .ReturnsAsync(existingRecord);

        // 復帰確認で「いいえ」を選択（キャプチャ破棄）
        _mockDialogService
            .Setup(x => x.ShowYesNoCancelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UI.Services.Abstractions.DialogResult.No);

        // Act
        var transition = await stateManager.HandleCheckOnAsync(viewModel, document);

        // Assert
        Assert.NotNull(transition);
        Assert.True(transition.IsChecked);
        Assert.Null(transition.CaptureFile);
        Assert.Equal("10", transition.TargetState);
    }

    /// <summary>
    /// 状態22（キャプチャあり、チェックOFF）でチェックON、復帰確認で「キャンセル」→ロールバック
    /// </summary>
    [Fact]
    public async Task HandleCheckOnAsync_既存キャプチャあり_キャンセル_ロールバック()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: false, captureFilePath: "captures/test.png");
        var document = CreateTestDocument();

        // 既存レコードあり（状態22: チェックOFF、キャプチャあり）
        var existingRecord = CreateTestCheckItemDocument(
            isChecked: false,
            captureFile: "captures/test.png");

        _mockRepository
            .Setup(x => x.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id))
            .ReturnsAsync(existingRecord);

        // 復帰確認で「キャンセル」を選択
        _mockDialogService
            .Setup(x => x.ShowYesNoCancelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UI.Services.Abstractions.DialogResult.Cancel);

        // Act
        var transition = await stateManager.HandleCheckOnAsync(viewModel, document);

        // Assert
        Assert.Null(transition); // キャンセルの場合はnullを返す
    }

    #endregion

    #region チェックOFF時のテスト

    /// <summary>
    /// 状態11（チェックON、キャプチャあり）でチェックOFF→状態22へ遷移
    /// </summary>
    [Fact]
    public async Task HandleCheckOffAsync_キャプチャあり_状態22へ遷移()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: true, captureFilePath: "captures/test.png");
        var document = CreateTestDocument();

        // 既存レコードあり（状態11: チェックON、キャプチャあり）
        var existingRecord = CreateTestCheckItemDocument(
            isChecked: true,
            captureFile: "captures/test.png");

        _mockRepository
            .Setup(x => x.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id))
            .ReturnsAsync(existingRecord);

        // Act
        var transition = await stateManager.HandleCheckOffAsync(viewModel, document);

        // Assert
        Assert.NotNull(transition);
        Assert.False(transition.IsChecked);
        Assert.Equal("captures/test.png", transition.CaptureFile);
        Assert.Equal("22", transition.TargetState);
    }

    /// <summary>
    /// 状態10（チェックON、キャプチャなし）でチェックOFF→状態20へ遷移
    /// </summary>
    [Fact]
    public async Task HandleCheckOffAsync_キャプチャなし_状態20へ遷移()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: true, captureFilePath: null);
        var document = CreateTestDocument();

        // 既存レコードあり（状態10: チェックON、キャプチャなし）
        var existingRecord = CreateTestCheckItemDocument(
            isChecked: true,
            captureFile: null);

        _mockRepository
            .Setup(x => x.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id))
            .ReturnsAsync(existingRecord);

        // Act
        var transition = await stateManager.HandleCheckOffAsync(viewModel, document);

        // Assert
        Assert.NotNull(transition);
        Assert.False(transition.IsChecked);
        Assert.Null(transition.CaptureFile);
        Assert.Equal("20", transition.TargetState);
    }

    #endregion

    #region キャプチャ保存のテスト

    /// <summary>
    /// 状態10（チェックON、キャプチャなし）でキャプチャ保存→状態11へ遷移
    /// </summary>
    [Fact]
    public async Task CommitCaptureAsync_新規キャプチャ保存_状態11へ遷移()
    {
        // Arrange
        var stateManager = CreateStateManager();

        var transition = new CheckItemTransition
        {
            CheckItemId = 1,
            DocumentId = 1,
            OriginalState = "10",
            TargetState = "10",
            IsChecked = true,
            CaptureFile = null
        };

        var captureFilePath = "captures/new_capture.png";

        // Act
        var result = await stateManager.CommitCaptureAsync(transition, captureFilePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("11", result.TargetState);
        Assert.Equal(captureFilePath, result.CaptureFile);
        Assert.True(result.IsChecked);
    }

    #endregion

    #region DB操作のテスト

    /// <summary>
    /// CheckItemDocumentレコードが存在しない場合→新規レコード作成
    /// </summary>
    [Fact]
    public async Task CommitTransitionAsync_新規レコード作成()
    {
        // Arrange
        var stateManager = CreateStateManager();

        var transition = new CheckItemTransition
        {
            CheckItemId = 1,
            DocumentId = 1,
            OriginalState = "00",
            TargetState = "10",
            IsChecked = true,
            CaptureFile = null,
            OriginalRecord = null // 新規レコード
        };

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<CheckItemDocument>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await stateManager.CommitTransitionAsync(transition);

        // Assert
        _mockRepository.Verify(
            x => x.AddAsync(It.Is<CheckItemDocument>(d =>
                d.CheckItemId == 1 &&
                d.DocumentId == 1 &&
                d.IsChecked == true &&
                d.CaptureFile == null)),
            Times.Once);

        _mockRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// CheckItemDocumentレコードが存在する場合→既存レコード更新
    /// </summary>
    [Fact]
    public async Task CommitTransitionAsync_既存レコード更新()
    {
        // Arrange
        var stateManager = CreateStateManager();

        var existingRecord = CreateTestCheckItemDocument(
            isChecked: false,
            captureFile: "captures/old.png");

        var transition = new CheckItemTransition
        {
            CheckItemId = 1,
            DocumentId = 1,
            OriginalState = "22",
            TargetState = "11",
            IsChecked = true,
            CaptureFile = "captures/old.png",
            OriginalRecord = existingRecord
        };

        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<CheckItemDocument>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await stateManager.CommitTransitionAsync(transition);

        // Assert
        _mockRepository.Verify(
            x => x.UpdateAsync(It.Is<CheckItemDocument>(d =>
                d.Id == existingRecord.Id &&
                d.IsChecked == true &&
                d.CaptureFile == "captures/old.png")),
            Times.Once);

        _mockRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region ロールバックのテスト

    /// <summary>
    /// 状態遷移が発生している場合→元の状態に復元
    /// </summary>
    [Fact]
    public async Task RollbackTransitionAsync_元の状態に復元()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var viewModel = CreateTestViewModel(isChecked: true);

        var existingRecord = CreateTestCheckItemDocument(
            isChecked: false,
            captureFile: "captures/old.png");

        var transition = new CheckItemTransition
        {
            CheckItemId = 1,
            DocumentId = 1,
            OriginalState = "22",
            TargetState = "11", // 変更後の状態
            IsChecked = true,
            CaptureFile = "captures/old.png",
            OriginalRecord = existingRecord
        };

        // Act
        await stateManager.RollbackTransitionAsync(transition, viewModel);

        // Assert
        Assert.Equal("22", transition.TargetState); // 元の状態に戻る
        Assert.False(viewModel.IsChecked); // ViewModelも元に戻る
        Assert.Equal("captures/old.png", viewModel.CaptureFilePath);
    }

    #endregion
}
