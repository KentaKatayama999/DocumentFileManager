using System.Text.Json;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Models;
using DocumentFileManager.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.Infrastructure.Services;

/// <summary>
/// checklist.json からチェック項目定義を読み込むサービス
/// </summary>
public class ChecklistLoader
{
    private readonly ILogger<ChecklistLoader> _logger;

    public ChecklistLoader(ILogger<ChecklistLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// JSONファイルからチェック項目定義を読み込む
    /// </summary>
    /// <param name="jsonFilePath">checklist.json のパス</param>
    /// <param name="timeoutSeconds">タイムアウト時間（秒）</param>
    /// <returns>チェック項目定義のリスト</returns>
    public async Task<List<CheckItemDefinition>> LoadAsync(string jsonFilePath, int timeoutSeconds = 10)
    {
        _logger.LogInformation("チェック項目定義を読み込みます: {FilePath} (タイムアウト: {Timeout}秒)", jsonFilePath, timeoutSeconds);

        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        try
        {
            // File.Existsもタイムアウト対象にする（ネットワークパスの場合に重要）
            var existsTask = Task.Run(() => File.Exists(jsonFilePath));
            bool fileExists;

            try
            {
                fileExists = await existsTask.WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                _logger.LogError("チェックリストファイルの存在確認がタイムアウトしました: {FilePath} ({Timeout}秒)", jsonFilePath, timeoutSeconds);
                throw new TimeoutException($"チェックリストファイルの存在確認がタイムアウトしました ({timeoutSeconds}秒): {jsonFilePath}");
            }

            if (!fileExists)
            {
                _logger.LogError("checklist.json が見つかりません: {FilePath}", jsonFilePath);
                throw new FileNotFoundException($"checklist.json が見つかりません: {jsonFilePath}");
            }

            // ファイル読み込みもタイムアウト対象にする
            var readTask = File.ReadAllTextAsync(jsonFilePath);
            string json;

            try
            {
                json = await readTask.WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                _logger.LogError("チェックリストファイルの読み込みがタイムアウトしました: {FilePath} ({Timeout}秒)", jsonFilePath, timeoutSeconds);
                throw new TimeoutException($"チェックリストファイルの読み込みがタイムアウトしました ({timeoutSeconds}秒): {jsonFilePath}");
            }

            var root = JsonSerializer.Deserialize<ChecklistRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (root == null || root.CheckItems == null || root.CheckItems.Count == 0)
            {
                _logger.LogWarning("チェック項目定義が空です");
                return new List<CheckItemDefinition>();
            }

            _logger.LogInformation("{Count} 件の大分類を読み込みました", root.CheckItems.Count);
            return root.CheckItems;
        }
        catch (TimeoutException)
        {
            // 既にログ出力済みなので、そのまま再スロー
            throw;
        }
    }

    /// <summary>
    /// チェック項目定義からCheckItemエンティティのリストを生成する
    /// </summary>
    /// <param name="definitions">チェック項目定義</param>
    /// <param name="parentPath">親のパス（再帰用）</param>
    /// <param name="parentId">親のID（再帰用）</param>
    /// <returns>CheckItemエンティティのフラットなリスト</returns>
    public List<CheckItem> ConvertToEntities(List<CheckItemDefinition> definitions, string? parentPath = null, int? parentId = null)
    {
        var result = new List<CheckItem>();

        foreach (var def in definitions)
        {
            // パスを生成（親パス + ラベル）
            var currentPath = string.IsNullOrEmpty(parentPath)
                ? def.Label
                : $"{parentPath}/{def.Label}";

            // CheckItemエンティティを作成
            var checkItem = new CheckItem
            {
                Path = currentPath,
                Label = def.Label,
                Status = def.Checked ? ItemStatus.Current : ItemStatus.Unspecified,
                ParentId = parentId
            };

            result.Add(checkItem);

            // 子要素を再帰的に処理
            if (def.Children != null && def.Children.Count > 0)
            {
                // 注意: このメソッドは親IDが確定していない状態で呼ばれるため、
                // 実際のDB保存時は2段階処理が必要（親を保存してからIDを取得し、子を保存）
                var children = ConvertToEntities(def.Children, currentPath, null);
                result.AddRange(children);
            }
        }

        return result;
    }

    /// <summary>
    /// チェック項目定義の統計情報を取得する
    /// </summary>
    public (int TotalCategories, int TotalItems, int CheckedItems) GetStatistics(List<CheckItemDefinition> definitions)
    {
        int categories = 0;
        int items = 0;
        int checkedItems = 0;

        void CountRecursive(List<CheckItemDefinition> defs)
        {
            foreach (var def in defs)
            {
                if (def.IsCategory)
                {
                    categories++;
                }
                else if (def.IsItem)
                {
                    items++;
                    if (def.Checked)
                    {
                        checkedItems++;
                    }
                }

                if (def.Children != null && def.Children.Count > 0)
                {
                    CountRecursive(def.Children);
                }
            }
        }

        CountRecursive(definitions);
        return (categories, items, checkedItems);
    }
}
