using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.Infrastructure.Repositories;

/// <summary>
/// 資料リポジトリの実装
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentManagerContext _context;
    private readonly ILogger<DocumentRepository> _logger;

    public DocumentRepository(DocumentManagerContext context, ILogger<DocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.LinkedCheckItems)
                .ThenInclude(cd => cd.CheckItem)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document?> GetByRelativePathAsync(string relativePath)
    {
        return await _context.Documents
            .Include(d => d.LinkedCheckItems)
                .ThenInclude(cd => cd.CheckItem)
            .FirstOrDefaultAsync(d => d.RelativePath == relativePath);
    }

    public async Task<List<Document>> GetAllAsync()
    {
        _logger.LogDebug("全資料の取得を開始します");
        var documents = await _context.Documents
            .Include(d => d.LinkedCheckItems)
                .ThenInclude(cd => cd.CheckItem)
            .OrderByDescending(d => d.AddedAt)
            .ToListAsync();
        _logger.LogInformation("{Count} 件の資料を取得しました", documents.Count);
        return documents;
    }

    public async Task<List<Document>> GetByFileTypeAsync(string fileType)
    {
        return await _context.Documents
            .Where(d => d.FileType == fileType)
            .Include(d => d.LinkedCheckItems)
                .ThenInclude(cd => cd.CheckItem)
            .OrderByDescending(d => d.AddedAt)
            .ToListAsync();
    }

    public async Task<List<Document>> SearchByFileNameAsync(string fileName)
    {
        return await _context.Documents
            .Where(d => EF.Functions.Like(d.FileName, $"%{fileName}%"))
            .Include(d => d.LinkedCheckItems)
                .ThenInclude(cd => cd.CheckItem)
            .OrderByDescending(d => d.AddedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Document document)
    {
        _logger.LogInformation("資料を追加します: {FileName}", document.FileName);
        await _context.Documents.AddAsync(document);
    }

    public async Task UpdateAsync(Document document)
    {
        _context.Documents.Update(document);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document != null)
        {
            _context.Documents.Remove(document);
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        _logger.LogDebug("データベース変更を保存します");
        var count = await _context.SaveChangesAsync();
        _logger.LogInformation("{Count} 件のレコードを保存しました", count);
        return count;
    }
}
