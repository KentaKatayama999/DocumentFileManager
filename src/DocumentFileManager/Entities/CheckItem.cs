using DocumentFileManager.ValueObjects;

namespace DocumentFileManager.Entities;

/// <summary>
/// チェックリスト項目を表すエンティティ
/// 構造（親子関係）と状態を統合管理
/// </summary>
public class CheckItem
{
    /// <summary>主キー</summary>
    public int Id { get; set; }

    /// <summary>
    /// 階層パス識別子（例: "設計図面/平面図"）
    /// 人間が読める形式で、デバッグが容易
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>表示ラベル（例: "平面図"）</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>チェック項目の状態</summary>
    public ItemStatus Status { get; set; } = ItemStatus.Unspecified;

    /// <summary>親項目ID（ルート項目の場合はnull）</summary>
    public int? ParentId { get; set; }

    /// <summary>親項目への参照</summary>
    public CheckItem? Parent { get; set; }

    /// <summary>子項目のコレクション</summary>
    public ICollection<CheckItem> Children { get; set; } = new List<CheckItem>();

    /// <summary>紐づけられた資料のコレクション</summary>
    public ICollection<CheckItemDocument> LinkedDocuments { get; set; } = new List<CheckItemDocument>();

    /// <summary>
    /// 状態を次の段階に進める
    /// 未指定 → 現行 → 改訂 → キャンセル → 未指定
    /// </summary>
    public void AdvanceStatus()
    {
        Status = Status switch
        {
            ItemStatus.Unspecified => ItemStatus.Current,
            ItemStatus.Current => ItemStatus.Revised,
            ItemStatus.Revised => ItemStatus.Cancelled,
            ItemStatus.Cancelled => ItemStatus.Unspecified,
            _ => ItemStatus.Unspecified
        };
    }

    /// <summary>
    /// 階層パスを生成する
    /// </summary>
    public string GeneratePath()
    {
        if (Parent == null)
        {
            return Label;
        }
        return $"{Parent.GeneratePath()}/{Label}";
    }
}
