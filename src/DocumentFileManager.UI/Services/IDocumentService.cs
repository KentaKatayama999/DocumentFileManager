using DocumentFileManager.Entities;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// 資料登録サービスのインターフェース
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// 資料を登録
    /// </summary>
    /// <param name="filePath">登録するファイルのパス</param>
    /// <returns>登録結果</returns>
    Task<DocumentRegistrationResult> RegisterDocumentAsync(string filePath);

    /// <summary>
    /// 複数の資料を一括登録
    /// </summary>
    /// <param name="filePaths">登録するファイルのパスリスト</param>
    /// <returns>登録結果リスト</returns>
    Task<List<DocumentRegistrationResult>> RegisterDocumentsAsync(IEnumerable<string> filePaths);
}

/// <summary>
/// 資料登録結果
/// </summary>
public class DocumentRegistrationResult
{
    /// <summary>
    /// 登録成功したかどうか
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 登録された資料（成功時のみ）
    /// </summary>
    public Document? Document { get; set; }

    /// <summary>
    /// スキップされたかどうか（既に登録済みの場合）
    /// </summary>
    public bool Skipped { get; set; }
}
