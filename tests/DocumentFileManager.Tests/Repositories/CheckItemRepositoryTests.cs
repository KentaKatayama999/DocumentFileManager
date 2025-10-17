using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocumentFileManager.Tests.Repositories;

/// <summary>
/// CheckItemRepositoryの統合テスト
/// </summary>
public class CheckItemRepositoryTests : IDisposable
{
    private readonly DocumentManagerContext _context;
    private readonly CheckItemRepository _repository;

    public CheckItemRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DocumentManagerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DocumentManagerContext(options);
        _repository = new CheckItemRepository(_context, NullLogger<CheckItemRepository>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_存在する項目を取得()
    {
        // Arrange
        var checkItem = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Current
        };
        await _context.CheckItems.AddAsync(checkItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(checkItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("設計図面", result.Label);
        Assert.Equal(ItemStatus.Current, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_存在しない項目_Nullを返す()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPathAsync_階層パスで項目を取得()
    {
        // Arrange
        var rootItem = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Current
        };
        await _context.CheckItems.AddAsync(rootItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPathAsync("設計図面");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("設計図面", result.Label);
    }

    [Fact]
    public async Task GetRootItemsAsync_ルート項目のみ取得()
    {
        // Arrange
        var rootItem1 = new CheckItem { Path = "設計図面", Label = "設計図面" };
        var rootItem2 = new CheckItem { Path = "施工図", Label = "施工図" };
        await _context.CheckItems.AddRangeAsync(rootItem1, rootItem2);
        await _context.SaveChangesAsync();

        var childItem = new CheckItem
        {
            Path = "設計図面/平面図",
            Label = "平面図",
            ParentId = rootItem1.Id
        };
        await _context.CheckItems.AddAsync(childItem);
        await _context.SaveChangesAsync();

        // Act
        var rootItems = await _repository.GetRootItemsAsync();

        // Assert
        Assert.Equal(2, rootItems.Count);
        Assert.Contains(rootItems, item => item.Label == "設計図面");
        Assert.Contains(rootItems, item => item.Label == "施工図");
    }

    [Fact]
    public async Task GetRootItemsAsync_階層構造を保持()
    {
        // Arrange
        var rootItem = new CheckItem { Path = "設計図面", Label = "設計図面" };
        await _context.CheckItems.AddAsync(rootItem);
        await _context.SaveChangesAsync();

        var childItem = new CheckItem
        {
            Path = "設計図面/平面図",
            Label = "平面図",
            ParentId = rootItem.Id
        };
        await _context.CheckItems.AddAsync(childItem);
        await _context.SaveChangesAsync();

        var grandchildItem = new CheckItem
        {
            Path = "設計図面/平面図/1階",
            Label = "1階",
            ParentId = childItem.Id
        };
        await _context.CheckItems.AddAsync(grandchildItem);
        await _context.SaveChangesAsync();

        // Act
        var rootItems = await _repository.GetRootItemsAsync();

        // Assert
        Assert.Single(rootItems);
        var root = rootItems[0];
        Assert.Equal("設計図面", root.Label);
        Assert.Single(root.Children);
        Assert.Equal("平面図", root.Children.First().Label);
    }

    [Fact]
    public async Task GetAllWithChildrenAsync_すべての項目を取得()
    {
        // Arrange
        var rootItem = new CheckItem { Path = "設計図面", Label = "設計図面" };
        await _context.CheckItems.AddAsync(rootItem);
        await _context.SaveChangesAsync();

        var childItem = new CheckItem
        {
            Path = "設計図面/平面図",
            Label = "平面図",
            ParentId = rootItem.Id
        };
        await _context.CheckItems.AddAsync(childItem);
        await _context.SaveChangesAsync();

        // Act
        var allItems = await _repository.GetAllWithChildrenAsync();

        // Assert
        Assert.Equal(2, allItems.Count);
    }

    [Fact]
    public async Task GetChildrenAsync_指定した親の子要素を取得()
    {
        // Arrange
        var rootItem = new CheckItem { Path = "設計図面", Label = "設計図面" };
        await _context.CheckItems.AddAsync(rootItem);
        await _context.SaveChangesAsync();

        var child1 = new CheckItem
        {
            Path = "設計図面/平面図",
            Label = "平面図",
            ParentId = rootItem.Id
        };
        var child2 = new CheckItem
        {
            Path = "設計図面/立面図",
            Label = "立面図",
            ParentId = rootItem.Id
        };
        await _context.CheckItems.AddRangeAsync(child1, child2);
        await _context.SaveChangesAsync();

        // Act
        var children = await _repository.GetChildrenAsync(rootItem.Id);

        // Assert
        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.Label == "平面図");
        Assert.Contains(children, c => c.Label == "立面図");
    }

    [Fact]
    public async Task AddAsync_新規項目を追加()
    {
        // Arrange
        var newItem = new CheckItem
        {
            Path = "新規項目",
            Label = "新規項目",
            Status = ItemStatus.Unspecified
        };

        // Act
        await _repository.AddAsync(newItem);
        await _repository.SaveChangesAsync();

        // Assert
        var saved = await _context.CheckItems.FindAsync(newItem.Id);
        Assert.NotNull(saved);
        Assert.Equal("新規項目", saved.Label);
    }

    [Fact]
    public async Task UpdateAsync_既存項目を更新()
    {
        // Arrange
        var checkItem = new CheckItem
        {
            Path = "設計図面",
            Label = "設計図面",
            Status = ItemStatus.Unspecified
        };
        await _context.CheckItems.AddAsync(checkItem);
        await _context.SaveChangesAsync();

        // Act
        checkItem.Status = ItemStatus.Current;
        await _repository.UpdateAsync(checkItem);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _context.CheckItems.FindAsync(checkItem.Id);
        Assert.NotNull(updated);
        Assert.Equal(ItemStatus.Current, updated.Status);
    }

    [Fact]
    public async Task DeleteAsync_項目を削除()
    {
        // Arrange
        var checkItem = new CheckItem
        {
            Path = "削除対象",
            Label = "削除対象"
        };
        await _context.CheckItems.AddAsync(checkItem);
        await _context.SaveChangesAsync();
        var id = checkItem.Id;

        // Act
        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _context.CheckItems.FindAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_存在しない項目_何も起こらない()
    {
        // Act & Assert - 例外が発生しないことを確認
        await _repository.DeleteAsync(999);
        await _repository.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_変更数を返す()
    {
        // Arrange
        var item1 = new CheckItem { Path = "項目1", Label = "項目1" };
        var item2 = new CheckItem { Path = "項目2", Label = "項目2" };
        await _repository.AddAsync(item1);
        await _repository.AddAsync(item2);

        // Act
        var changeCount = await _repository.SaveChangesAsync();

        // Assert
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public async Task GetByIdAsync_LinkedDocumentsを含む()
    {
        // Arrange
        var checkItem = new CheckItem { Path = "項目", Label = "項目" };
        var document = new Document
        {
            FileName = "test.pdf",
            RelativePath = "test.pdf",
            FileType = "pdf"
        };
        await _context.CheckItems.AddAsync(checkItem);
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        var link = new CheckItemDocument
        {
            CheckItemId = checkItem.Id,
            DocumentId = document.Id,
            LinkedAt = DateTime.UtcNow
        };
        await _context.CheckItemDocuments.AddAsync(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(checkItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.LinkedDocuments);
        Assert.Equal("test.pdf", result.LinkedDocuments.First().Document.FileName);
    }
}
