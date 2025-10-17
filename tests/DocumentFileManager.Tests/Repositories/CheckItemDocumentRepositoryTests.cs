using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocumentFileManager.Tests.Repositories;

/// <summary>
/// CheckItemDocumentRepositoryの統合テスト
/// </summary>
public class CheckItemDocumentRepositoryTests : IDisposable
{
    private readonly DocumentManagerContext _context;
    private readonly CheckItemDocumentRepository _repository;

    public CheckItemDocumentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DocumentManagerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DocumentManagerContext(options);
        _repository = new CheckItemDocumentRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<(CheckItem checkItem, Document document)> CreateTestDataAsync()
    {
        var checkItem = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面"
        };
        var document = new Document
        {
            FileName = "test.pdf",
            RelativePath = "docs/test.pdf",
            FileType = "pdf"
        };

        await _context.CheckItems.AddAsync(checkItem);
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        return (checkItem, document);
    }

    [Fact]
    public async Task GetByIdAsync_存在する紐づけを取得()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(link.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(checkItem.Id, result.CheckItemId);
        Assert.Equal(document.Id, result.DocumentId);
        Assert.NotNull(result.CheckItem);
        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task GetByIdAsync_存在しない紐づけ_Nullを返す()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCheckItemIdAsync_チェック項目に紐づく資料を取得()
    {
        // Arrange
        var (checkItem, document1) = await CreateTestDataAsync();
        var document2 = new Document
        {
            FileName = "test2.pdf",
            RelativePath = "docs/test2.pdf",
            FileType = "pdf"
        };
        await _context.Documents.AddAsync(document2);
        await _context.SaveChangesAsync();

        var link1 = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document1.Id,
            LinkedAt = DateTime.UtcNow.AddHours(-1)
        };
        var link2 = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document2.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddRangeAsync(link1, link2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByCheckItemIdAsync(checkItem.Id);

        // Assert
        Assert.Equal(2, results.Count);
        // 新しい順にソートされていることを確認
        Assert.Equal(document2.Id, results[0].DocumentId);
        Assert.Equal(document1.Id, results[1].DocumentId);
    }

    [Fact]
    public async Task GetByDocumentIdAsync_資料に紐づくチェック項目を取得()
    {
        // Arrange
        var (checkItem1, document) = await CreateTestDataAsync();
        var checkItem2 = new CheckItem
        {
            Path = "施工図",
            Label = "施工図"
        };
        await _context.CheckItems.AddAsync(checkItem2);
        await _context.SaveChangesAsync();

        var link1 = new CheckItemDocument
        {
            CheckItemId = checkItem1.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow.AddHours(-1)
        };
        var link2 = new CheckItemDocument
        {
            CheckItemId = checkItem2.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddRangeAsync(link1, link2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByDocumentIdAsync(document.Id);

        // Assert
        Assert.Equal(2, results.Count);
        // 新しい順にソートされていることを確認
        Assert.Equal(checkItem2.Id, results[0].CheckItemId);
        Assert.Equal(checkItem1.Id, results[1].CheckItemId);
    }

    [Fact]
    public async Task GetByDocumentAndCheckItemAsync_特定の紐づけを取得()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDocumentAndCheckItemAsync(document.Id, checkItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(checkItem.Id, result.CheckItemId);
        Assert.Equal(document.Id, result.DocumentId);
    }

    [Fact]
    public async Task GetByDocumentAndCheckItemAsync_存在しない組み合わせ_Nullを返す()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();

        // Act
        var result = await _repository.GetByDocumentAndCheckItemAsync(document.Id, 999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_新規紐づけを追加()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var newLink = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(newLink);
        await _repository.SaveChangesAsync();

        // Assert
        var saved = await _context.CheckItemDocuments.FindAsync(newLink.Id);
        Assert.NotNull(saved);
        Assert.Equal(checkItem.Id, saved.CheckItemId);
        Assert.Equal(document.Id, saved.DocumentId);
    }

    [Fact]
    public async Task DeleteAsync_紐づけを削除()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();
        var linkId = link.Id;

        // Act
        await _repository.DeleteAsync(linkId);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _context.CheckItemDocuments.FindAsync(linkId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_存在しない紐づけ_何も起こらない()
    {
        // Act & Assert - 例外が発生しないことを確認
        await _repository.DeleteAsync(999);
        await _repository.SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteLinkAsync_特定の紐づけを削除()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteLinkAsync(checkItem.Id, document.Id);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _context.CheckItemDocuments.FindAsync(link.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteLinkAsync_存在しない組み合わせ_何も起こらない()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();

        // Act & Assert - 例外が発生しないことを確認
        await _repository.DeleteLinkAsync(999, document.Id);
        await _repository.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateCaptureFileAsync_キャプチャファイルパスを更新()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow,
            CaptureFile = null
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();

        // Act
        var capturePath = "captures/screenshot.png";
        await _repository.UpdateCaptureFileAsync(link.Id, capturePath);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _context.CheckItemDocuments.FindAsync(link.Id);
        Assert.NotNull(updated);
        Assert.Equal(capturePath, updated.CaptureFile);
    }

    [Fact]
    public async Task UpdateCaptureFileAsync_Nullに更新()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow,
            CaptureFile = "captures/old.png"
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();

        // Act
        await _repository.UpdateCaptureFileAsync(link.Id, null);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _context.CheckItemDocuments.FindAsync(link.Id);
        Assert.NotNull(updated);
        Assert.Null(updated.CaptureFile);
    }

    [Fact]
    public async Task UpdateCaptureFileAsync_存在しないID_何も起こらない()
    {
        // Act & Assert - 例外が発生しないことを確認
        await _repository.UpdateCaptureFileAsync(999, "captures/test.png");
        await _repository.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_変更数を返す()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link1 = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(link1);

        // Act
        var changeCount = await _repository.SaveChangesAsync();

        // Assert
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public async Task CascadeDelete_CheckItem削除時に紐づけも削除()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();
        var linkId = link.Id;

        // Act - CheckItemを削除
        _context.CheckItems.Remove(checkItem);
        await _context.SaveChangesAsync();

        // Assert - 紐づけも削除されることを確認
        var deletedLink = await _context.CheckItemDocuments.FindAsync(linkId);
        Assert.Null(deletedLink);
    }

    [Fact]
    public async Task CascadeDelete_Document削除時に紐づけも削除()
    {
        // Arrange
        var (checkItem, document) = await CreateTestDataAsync();
        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();
        var linkId = link.Id;

        // Act - Documentを削除
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        // Assert - 紐づけも削除されることを確認
        var deletedLink = await _context.CheckItemDocuments.FindAsync(linkId);
        Assert.Null(deletedLink);
    }
}
