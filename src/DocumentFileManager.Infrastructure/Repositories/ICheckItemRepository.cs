using DocumentFileManager.Entities;

namespace DocumentFileManager.Infrastructure.Repositories;

/// <summary>
/// チェック項目リポジトリのインターフェース
/// </summary>
public interface ICheckItemRepository
{
    /// <summary>IDでチェック項目を取得</summary>
    Task<CheckItem?> GetByIdAsync(int id);

    /// <summary>階層パスでチェック項目を取得</summary>
    Task<CheckItem?> GetByPathAsync(string path);

    /// <summary>すべてのルート項目を取得（親がnull）</summary>
    Task<List<CheckItem>> GetRootItemsAsync();

    /// <summary>すべてのチェック項目を階層構造で取得</summary>
    Task<List<CheckItem>> GetAllWithChildrenAsync();

    /// <summary>指定した親の子項目を取得</summary>
    Task<List<CheckItem>> GetChildrenAsync(int parentId);

    /// <summary>チェック項目を追加</summary>
    Task AddAsync(CheckItem checkItem);

    /// <summary>チェック項目を更新</summary>
    Task UpdateAsync(CheckItem checkItem);

    /// <summary>チェック項目を削除</summary>
    Task DeleteAsync(int id);

    /// <summary>変更を保存</summary>
    Task<int> SaveChangesAsync();
}
