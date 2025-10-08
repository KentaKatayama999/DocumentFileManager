using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocumentFileManager.Infrastructure.Repositories;

/// <summary>
/// チェック項目-資料紐づけリポジトリの実装
/// </summary>
public class CheckItemDocumentRepository : ICheckItemDocumentRepository
{
    private readonly DocumentManagerContext _context;

    public CheckItemDocumentRepository(DocumentManagerContext context)
    {
        _context = context;
    }

    public async Task<CheckItemDocument?> GetByIdAsync(int id)
    {
        return await _context.CheckItemDocuments
            .Include(cd => cd.CheckItem)
            .Include(cd => cd.Document)
            .FirstOrDefaultAsync(cd => cd.Id == id);
    }

    public async Task<List<CheckItemDocument>> GetByCheckItemIdAsync(int checkItemId)
    {
        return await _context.CheckItemDocuments
            .Where(cd => cd.CheckItemId == checkItemId)
            .Include(cd => cd.Document)
            .OrderByDescending(cd => cd.LinkedAt)
            .ToListAsync();
    }

    public async Task<List<CheckItemDocument>> GetByDocumentIdAsync(int documentId)
    {
        return await _context.CheckItemDocuments
            .Where(cd => cd.DocumentId == documentId)
            .Include(cd => cd.CheckItem)
            .OrderByDescending(cd => cd.LinkedAt)
            .ToListAsync();
    }

    public async Task<CheckItemDocument?> GetByDocumentAndCheckItemAsync(int documentId, int checkItemId)
    {
        return await _context.CheckItemDocuments
            .FirstOrDefaultAsync(cd => cd.DocumentId == documentId && cd.CheckItemId == checkItemId);
    }

    public async Task AddAsync(CheckItemDocument checkItemDocument)
    {
        await _context.CheckItemDocuments.AddAsync(checkItemDocument);
    }

    public async Task DeleteAsync(int id)
    {
        var link = await _context.CheckItemDocuments.FindAsync(id);
        if (link != null)
        {
            _context.CheckItemDocuments.Remove(link);
        }
    }

    public async Task DeleteLinkAsync(int checkItemId, int documentId)
    {
        var link = await _context.CheckItemDocuments
            .FirstOrDefaultAsync(cd => cd.CheckItemId == checkItemId && cd.DocumentId == documentId);

        if (link != null)
        {
            _context.CheckItemDocuments.Remove(link);
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
