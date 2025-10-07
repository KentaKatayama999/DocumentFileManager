using System.Text.Json.Serialization;

namespace DocumentFileManager.Infrastructure.Models;

/// <summary>
/// JSONファイルから読み込むチェック項目の定義
/// </summary>
public class CheckItemDefinition
{
    /// <summary>表示ラベル</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 種別: "category" (分類) または "item" (チェック項目)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "category";

    /// <summary>
    /// チェック状態（type="item"の場合のみ有効）
    /// true の場合、Status = Current になる
    /// </summary>
    [JsonPropertyName("checked")]
    public bool Checked { get; set; } = false;

    /// <summary>子要素のリスト</summary>
    [JsonPropertyName("children")]
    public List<CheckItemDefinition>? Children { get; set; }

    /// <summary>分類かどうか</summary>
    [JsonIgnore]
    public bool IsCategory => Type == "category";

    /// <summary>チェック項目かどうか</summary>
    [JsonIgnore]
    public bool IsItem => Type == "item";
}

/// <summary>
/// checklist.json のルート要素
/// </summary>
public class ChecklistRoot
{
    /// <summary>チェック項目の定義リスト</summary>
    [JsonPropertyName("checkItems")]
    public List<CheckItemDefinition> CheckItems { get; set; } = new();
}
