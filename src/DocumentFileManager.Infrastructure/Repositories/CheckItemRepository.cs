using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.Infrastructure.Repositories;

/// <summary>
/// チェック項目リポジトリの実装
/// </summary>
public class CheckItemRepository : ICheckItemRepository
{
    private readonly DocumentManagerContext _context;
    private readonly ILogger<CheckItemRepository> _logger;

    public CheckItemRepository(DocumentManagerContext context, ILogger<CheckItemRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CheckItem?> GetByIdAsync(int id)
    {
        return await _context.CheckItems
            .Include(c => c.Children)
            .Include(c => c.LinkedDocuments)
                .ThenInclude(cd => cd.Document)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CheckItem?> GetByPathAsync(string path)
    {
        return await _context.CheckItems
            .Include(c => c.Children)
            .Include(c => c.LinkedDocuments)
                .ThenInclude(cd => cd.Document)
            .FirstOrDefaultAsync(c => c.Path == path);
    }

    public async Task<List<CheckItem>> GetRootItemsAsync()
    {
        _logger.LogDebug("ルート項目の取得を開始します（全階層を含む）");

        // すべてのチェック項目を取得（EF Coreが親子関係を自動構築）
        var allItems = await _context.CheckItems
            .Include(c => c.LinkedDocuments)
                .ThenInclude(cd => cd.Document)
            .ToListAsync();

        _logger.LogDebug("{Count} 件の全チェック項目を取得しました", allItems.Count);

        // メモリ上で親子関係を構築（EF Core Change Trackerが自動で行う）
        var rootItems = allItems.Where(c => c.ParentId == null).ToList();

        _logger.LogInformation("{Count} 件のルート項目を取得しました", rootItems.Count);
        return rootItems;
    }

    public async Task<List<CheckItem>> GetAllWithChildrenAsync()
    {
        _logger.LogDebug("全チェック項目の取得を開始します");
        // すべての項目を取得（EF Coreが自動的に親子関係を構築）
        var checkItems = await _context.CheckItems
            .Include(c => c.Children)
            .Include(c => c.LinkedDocuments)
                .ThenInclude(cd => cd.Document)
            .ToListAsync();
        _logger.LogInformation("{Count} 件のチェック項目を取得しました", checkItems.Count);
        return checkItems;
    }

    public async Task<List<CheckItem>> GetChildrenAsync(int parentId)
    {
        return await _context.CheckItems
            .Where(c => c.ParentId == parentId)
            .Include(c => c.Children)
            .Include(c => c.LinkedDocuments)
                .ThenInclude(cd => cd.Document)
            .ToListAsync();
    }

    public async Task AddAsync(CheckItem checkItem)
    {
        await _context.CheckItems.AddAsync(checkItem);
    }

    public async Task UpdateAsync(CheckItem checkItem)
    {
        _context.CheckItems.Update(checkItem);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _context.CheckItems.FindAsync(id);
        if (item != null)
        {
            _context.CheckItems.Remove(item);
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
