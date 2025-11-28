namespace DocumentFileManager.Entities;

/// <summary>
/// チェック項目と資料の紐づけを管理するエンティティ
/// 多対多の関係を表現し、履歴管理を可能にする
/// </summary>
public class CheckItemDocument
{
    /// <summary>主キー</summary>
    public int Id { get; set; }

    /// <summary>チェック項目ID</summary>
    public int CheckItemId { get; set; }

    /// <summary>チェック項目への参照</summary>
    public CheckItem CheckItem { get; set; } = null!;

    /// <summary>資料ID</summary>
    public int DocumentId { get; set; }

    /// <summary>資料への参照</summary>
    public Document Document { get; set; } = null!;

    /// <summary>紐づけ日時（UTC）</summary>
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// チェック状態
    /// true: チェックON、false: チェックOFF（履歴として保持）
    /// </summary>
    public bool IsChecked { get; set; } = true;

    /// <summary>
    /// 画面キャプチャのファイルパス（相対パス）
    /// 紐づけ時にスクリーンショットを保存した場合に設定
    /// </summary>
    public string? CaptureFile { get; set; }

    /// <summary>
    /// キャプチャファイルの絶対パスを取得する
    /// </summary>
    /// <param name="projectRoot">プロジェクトルートの絶対パス</param>
    /// <returns>キャプチャファイルの絶対パス（nullの場合はnull）</returns>
    public string? GetCaptureAbsolutePath(string projectRoot)
    {
        if (string.IsNullOrEmpty(CaptureFile))
        {
            return null;
        }
        return Path.Combine(projectRoot, CaptureFile);
    }

    /// <summary>
    /// キャプチャファイルが存在するか確認する
    /// </summary>
    /// <param name="projectRoot">プロジェクトルートの絶対パス</param>
    /// <returns>キャプチャファイルが存在する場合true</returns>
    public bool CaptureExists(string projectRoot)
    {
        var absolutePath = GetCaptureAbsolutePath(projectRoot);
        return absolutePath != null && File.Exists(absolutePath);
    }
}
