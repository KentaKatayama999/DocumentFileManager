using DocumentFileManager.Entities;

namespace DocumentFileManager.UI.Models;

/// <summary>
/// 資料一覧表示用のラッパークラス
/// フィルタリング時に最新/古いファイルの区別を表示するために使用
/// </summary>
public class DocumentDisplayItem
{
    /// <summary>元のDocumentエンティティ</summary>
    public Document Document { get; }

    /// <summary>最新の紐づけかどうか</summary>
    public bool IsLatest { get; }

    /// <summary>ファイル名（Documentから委譲）</summary>
    public string FileName => Document.FileName;

    /// <summary>ファイルタイプ（Documentから委譲）</summary>
    public string FileType => Document.FileType;

    /// <summary>追加日時（Documentから委譲）</summary>
    public DateTime AddedAt => Document.AddedAt;

    /// <summary>ID（Documentから委譲）</summary>
    public int Id => Document.Id;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="document">元のDocumentエンティティ</param>
    /// <param name="isLatest">最新の紐づけかどうか</param>
    public DocumentDisplayItem(Document document, bool isLatest = true)
    {
        Document = document;
        IsLatest = isLatest;
    }
}
