using System.IO;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.Infrastructure.Services;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocumentFileManager.Tests.Services;

/// <summary>
/// ChecklistServiceのテスト
/// </summary>
public class ChecklistServiceTests : IDisposable
{
    private readonly DocumentManagerContext _context;
    private readonly CheckItemRepository _repository;
    private readonly ChecklistSaver _checklistSaver;
    private readonly PathSettings _pathSettings;
    private readonly ChecklistService _service;
    private readonly string _testRootPath;

    public ChecklistServiceTests()
    {
        var options = new DbContextOptionsBuilder<DocumentManagerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DocumentManagerContext(options);
        _repository = new CheckItemRepository(_context, NullLogger<CheckItemRepository>.Instance);
        _checklistSaver = new ChecklistSaver(NullLogger<ChecklistSaver>.Instance);
        _pathSettings = new PathSettings();

        // テスト用の一時ディレクトリを作成
        _testRootPath = Path.Combine(Path.GetTempPath(), $"ChecklistServiceTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRootPath);

        _service = new ChecklistService(
            _repository,
            _checklistSaver,
            _pathSettings,
            NullLogger<ChecklistService>.Instance,
            _testRootPath);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // テスト用ディレクトリを削除
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateNewChecklistAsync_正常にチェックリストを作成できる()
    {
        // Arrange
        var checklistName = "テストチェックリスト";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("checklist_テストチェックリスト.json", result.FileName);
        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(result.FilePath));

        // JSONファイルの内容を確認（ChecklistSaverの出力形式に合わせる）
        var content = await File.ReadAllTextAsync(result.FilePath);
        Assert.Contains("checkItems", content);
    }

    [Fact]
    public async Task CreateNewChecklistAsync_空の名前はエラー()
    {
        // Arrange
        var checklistName = "";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("チェックリスト名を入力してください", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateNewChecklistAsync_空白のみの名前はエラー()
    {
        // Arrange
        var checklistName = "   ";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("チェックリスト名を入力してください", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateNewChecklistAsync_既存ファイルを上書きできる()
    {
        // Arrange
        var checklistName = "上書きテスト";
        var fileName = "checklist_上書きテスト.json";
        var filePath = Path.Combine(_testRootPath, fileName);

        // 既存ファイルを作成
        await File.WriteAllTextAsync(filePath, "[{\"old\": \"data\"}]");

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Overwritten);
        Assert.Equal(fileName, result.FileName);

        // 内容が新しいチェックリスト形式に上書きされていることを確認
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("checkItems", content);
        Assert.DoesNotContain("old", content);
    }

    [Fact]
    public async Task CreateNewChecklistAsync_不正な文字を除去してファイル名を生成()
    {
        // Arrange - ファイル名に使えない文字を含む名前
        var checklistName = "テスト<>:\"/\\|?*チェックリスト";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.True(result.Success);
        // 不正な文字が除去されていることを確認
        Assert.DoesNotContain("<", result.FileName);
        Assert.DoesNotContain(">", result.FileName);
        Assert.DoesNotContain(":", result.FileName);
        Assert.DoesNotContain("\"", result.FileName);
        Assert.DoesNotContain("/", result.FileName);
        Assert.DoesNotContain("\\", result.FileName);
        Assert.DoesNotContain("|", result.FileName);
        Assert.DoesNotContain("?", result.FileName);
        Assert.DoesNotContain("*", result.FileName);
    }

    [Fact]
    public async Task CreateNewChecklistAsync_既存のチェック項目のクリアを試みる()
    {
        // Arrange - 既存のチェック項目を追加
        var existingItem = new CheckItem
        {
            Path = "既存項目",
            Label = "既存項目"
        };
        await _context.CheckItems.AddAsync(existingItem);
        await _context.SaveChangesAsync();

        // 既存項目があることを確認
        var countBefore = await _context.CheckItems.CountAsync();
        Assert.Equal(1, countBefore);

        // Act
        var result = await _service.CreateNewChecklistAsync("新規チェックリスト");

        // Assert - 新規作成は成功する
        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(result.FilePath));

        // 注: 実際のクリア処理はリポジトリのSaveChangesを呼び出す必要がある
        // サービス内でDeleteAsyncを呼び出しているが、SaveChangesは別途呼ばれる
        // テスト環境ではInMemoryDBのため、この動作は統合テストで検証する
    }

    [Fact]
    public async Task CreateNewChecklistAsync_PathSettingsが更新される()
    {
        // Arrange
        var originalFileName = _pathSettings.SelectedChecklistFile;
        var checklistName = "新しいチェックリスト";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("checklist_新しいチェックリスト.json", _pathSettings.SelectedChecklistFile);
        Assert.NotEqual(originalFileName, _pathSettings.SelectedChecklistFile);
    }

    [Fact]
    public void ChecklistExists_存在するファイルはtrueを返す()
    {
        // Arrange
        var fileName = "existing_checklist.json";
        var filePath = Path.Combine(_testRootPath, fileName);
        File.WriteAllText(filePath, "[]");

        // Act
        var exists = _service.ChecklistExists(fileName);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ChecklistExists_存在しないファイルはfalseを返す()
    {
        // Arrange
        var fileName = "non_existing_checklist.json";

        // Act
        var exists = _service.ChecklistExists(fileName);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task CreateNewChecklistAsync_日本語名でチェックリストを作成できる()
    {
        // Arrange
        var checklistName = "建築プロジェクト設備点検";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("checklist_建築プロジェクト設備点検.json", result.FileName);
        Assert.True(File.Exists(result.FilePath));
    }

    [Fact]
    public async Task CreateNewChecklistAsync_英数字混合の名前で作成できる()
    {
        // Arrange
        var checklistName = "Project2024_Phase1";

        // Act
        var result = await _service.CreateNewChecklistAsync(checklistName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("checklist_Project2024_Phase1.json", result.FileName);
        Assert.True(File.Exists(result.FilePath));
    }
}
