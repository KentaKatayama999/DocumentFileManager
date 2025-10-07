using System.Diagnostics;

namespace DocumentFileManager.Entities;

/// <summary>
/// 資料ファイルを表すエンティティ
/// プロジェクト内の技術資料（PDF等）を管理
/// </summary>
public class Document
{
    /// <summary>主キー</summary>
    public int Id { get; set; }

    /// <summary>ファイル名（例: "設計書_rev1.pdf"）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトルートからの相対パス
    /// プロジェクト移動時の可搬性を確保
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>ファイル拡張子（例: ".pdf"）</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>登録日時（UTC）</summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>紐づけられたチェック項目のコレクション</summary>
    public ICollection<CheckItemDocument> LinkedCheckItems { get; set; } = new List<CheckItemDocument>();

    /// <summary>
    /// 絶対パスを取得する
    /// </summary>
    /// <param name="projectRoot">プロジェクトルートの絶対パス</param>
    /// <returns>ファイルの絶対パス</returns>
    public string GetAbsolutePath(string projectRoot)
    {
        return Path.Combine(projectRoot, RelativePath);
    }

    /// <summary>
    /// ファイルが存在するか確認する
    /// </summary>
    /// <param name="projectRoot">プロジェクトルートの絶対パス</param>
    /// <returns>ファイルが存在する場合true</returns>
    public bool Exists(string projectRoot)
    {
        var absolutePath = GetAbsolutePath(projectRoot);
        return File.Exists(absolutePath);
    }

    /// <summary>
    /// 外部アプリケーションでファイルを開く
    /// </summary>
    /// <param name="projectRoot">プロジェクトルートの絶対パス</param>
    public void Open(string projectRoot)
    {
        var absolutePath = GetAbsolutePath(projectRoot);
        if (!Exists(projectRoot))
        {
            throw new FileNotFoundException($"ファイルが見つかりません: {absolutePath}");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = absolutePath,
            UseShellExecute = true
        });
    }
}
