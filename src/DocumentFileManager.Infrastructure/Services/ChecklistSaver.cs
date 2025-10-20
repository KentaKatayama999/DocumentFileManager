using System.Text.Json;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.Infrastructure.Services;

/// <summary>
/// チェック項目定義を checklist.json に保存するサービス
/// </summary>
public class ChecklistSaver
{
    private readonly ILogger<ChecklistSaver> _logger;

    public ChecklistSaver(ILogger<ChecklistSaver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// CheckItemエンティティのリストをJSONファイルに保存する
    /// </summary>
    /// <param name="checkItems">チェック項目エンティティのリスト</param>
    /// <param name="jsonFilePath">checklist.json のパス</param>
    public async Task SaveAsync(List<CheckItem> checkItems, string jsonFilePath)
    {
        _logger.LogInformation("チェック項目定義を保存します: {FilePath}", jsonFilePath);

        // ルート項目のみを抽出
        var rootItems = checkItems.Where(item => item.ParentId == null).ToList();

        // CheckItemDefinitionに変換
        var definitions = ConvertToDefinitions(rootItems, checkItems);

        // ChecklistRootに格納
        var root = new ChecklistRoot
        {
            CheckItems = definitions
        };

        // JSON形式で保存
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(root, options);
        await File.WriteAllTextAsync(jsonFilePath, json);

        _logger.LogInformation("チェック項目定義を保存しました: {Count} 件のルート項目", rootItems.Count);
    }

    /// <summary>
    /// CheckItemエンティティをCheckItemDefinitionに変換する（階層構造を再構築）
    /// </summary>
    private List<CheckItemDefinition> ConvertToDefinitions(List<CheckItem> items, List<CheckItem> allItems)
    {
        var result = new List<CheckItemDefinition>();

        foreach (var item in items)
        {
            // 子項目を取得
            var children = allItems.Where(c => c.ParentId == item.Id).ToList();

            var definition = new CheckItemDefinition
            {
                Label = item.Label,
                Type = children.Any() ? "category" : "item",
                Checked = item.Status == ValueObjects.ItemStatus.Current
                    || item.Status == ValueObjects.ItemStatus.Revised,
                Children = children.Any() ? ConvertToDefinitions(children, allItems) : null
            };

            result.Add(definition);
        }

        return result;
    }
}
