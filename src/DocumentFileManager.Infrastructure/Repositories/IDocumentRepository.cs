using DocumentFileManager.Entities;

namespace DocumentFileManager.Infrastructure.Repositories;

/// <summary>
/// 資料リポジトリのインターフェース
/// </summary>
public interface IDocumentRepository
{
    /// <summary>IDで資料を取得</summary>
    Task<Document?> GetByIdAsync(int id);

    /// <summary>相対パスで資料を取得</summary>
    Task<Document?> GetByRelativePathAsync(string relativePath);

    /// <summary>すべての資料を取得</summary>
    Task<List<Document>> GetAllAsync();

    /// <summary>ファイルタイプで資料を検索</summary>
    Task<List<Document>> GetByFileTypeAsync(string fileType);

    /// <summary>ファイル名で資料を検索（部分一致）</summary>
    Task<List<Document>> SearchByFileNameAsync(string fileName);

    /// <summary>資料を追加</summary>
    Task AddAsync(Document document);

    /// <summary>資料を更新</summary>
    Task UpdateAsync(Document document);

    /// <summary>資料を削除</summary>
    Task DeleteAsync(int id);

    /// <summary>変更を保存</summary>
    Task<int> SaveChangesAsync();
}
