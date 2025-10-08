using DocumentFileManager.Entities;

namespace DocumentFileManager.Infrastructure.Repositories;

/// <summary>
/// チェック項目-資料紐づけリポジトリのインターフェース
/// </summary>
public interface ICheckItemDocumentRepository
{
    /// <summary>IDで紐づけを取得</summary>
    Task<CheckItemDocument?> GetByIdAsync(int id);

    /// <summary>チェック項目に紐づけられた資料を取得</summary>
    Task<List<CheckItemDocument>> GetByCheckItemIdAsync(int checkItemId);

    /// <summary>資料に紐づけられたチェック項目を取得</summary>
    Task<List<CheckItemDocument>> GetByDocumentIdAsync(int documentId);

    /// <summary>資料とチェック項目の紐づけを取得</summary>
    Task<CheckItemDocument?> GetByDocumentAndCheckItemAsync(int documentId, int checkItemId);

    /// <summary>紐づけを追加</summary>
    Task AddAsync(CheckItemDocument checkItemDocument);

    /// <summary>紐づけを削除</summary>
    Task DeleteAsync(int id);

    /// <summary>チェック項目と資料の紐づけを削除</summary>
    Task DeleteLinkAsync(int checkItemId, int documentId);

    /// <summary>変更を保存</summary>
    Task<int> SaveChangesAsync();
}
