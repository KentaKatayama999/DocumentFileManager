namespace DocumentFileManager.ValueObjects;

/// <summary>
/// チェック項目の状態を表す列挙型
/// </summary>
public enum ItemStatus
{
    /// <summary>未指定</summary>
    Unspecified = 0,

    /// <summary>現行</summary>
    Current = 1,

    /// <summary>改訂</summary>
    Revised = 2,

    /// <summary>キャンセル</summary>
    Cancelled = 3
}
