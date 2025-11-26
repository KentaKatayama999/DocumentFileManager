namespace DocumentFileManager.UI.Services;

/// <summary>
/// チェックリスト管理サービスのインターフェース
/// </summary>
public interface IChecklistService
{
    /// <summary>
    /// 新規チェックリストを作成
    /// </summary>
    /// <param name="checklistName">チェックリスト名</param>
    /// <returns>作成結果</returns>
    Task<ChecklistCreationResult> CreateNewChecklistAsync(string checklistName);

    /// <summary>
    /// 指定したファイル名のチェックリストが存在するかどうかを確認
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <returns>存在する場合はtrue</returns>
    bool ChecklistExists(string fileName);
}

/// <summary>
/// チェックリスト作成結果
/// </summary>
public class ChecklistCreationResult
{
    /// <summary>
    /// 作成成功したかどうか
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 作成されたファイル名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 作成されたファイルの絶対パス
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 上書きされたかどうか
    /// </summary>
    public bool Overwritten { get; set; }
}
