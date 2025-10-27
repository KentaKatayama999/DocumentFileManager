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
    /// <param name="maxRetries">最大リトライ回数（デフォルト10回）</param>
    /// <param name="retryIntervalMs">リトライ間隔（ミリ秒、デフォルト1000ms = 1秒）</param>
    /// <returns>チェック項目定義のリスト</returns>
    public async Task<List<CheckItemDefinition>> LoadAsync(string jsonFilePath, int maxRetries = 10, int retryIntervalMs = 1000)
    {
        _logger.LogInformation("チェック項目定義を読み込みます: {FilePath} (最大{MaxRetries}回試行、{Interval}ms間隔)",
            jsonFilePath, maxRetries, retryIntervalMs);

        // ファイルの存在確認を1秒おきに10回試行
        bool fileExists = false;
        for (int i = 1; i <= maxRetries; i++)
        {
            _logger.LogInformation("ファイル存在確認: {Attempt}/{MaxRetries}回目", i, maxRetries);

            try
            {
                var existsTask = Task.Run(() => File.Exists(jsonFilePath));
                fileExists = await existsTask.WaitAsync(TimeSpan.FromMilliseconds(retryIntervalMs)).ConfigureAwait(false);

                if (fileExists)
                {
                    _logger.LogInformation("ファイルが見つかりました（{Attempt}回目）", i);
                    break;
                }
                else
                {
                    _logger.LogWarning("ファイルが見つかりません（{Attempt}/{MaxRetries}回目）", i, maxRetries);
                }
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("ファイル存在確認がタイムアウトしました（{Attempt}/{MaxRetries}回目、{Interval}ms）",
                    i, maxRetries, retryIntervalMs);
            }

            // 最後の試行でない場合は待機
            if (i < maxRetries)
            {
                await Task.Delay(retryIntervalMs).ConfigureAwait(false);
            }
        }

        if (!fileExists)
        {
            _logger.LogError("checklist.json が見つかりません（{MaxRetries}回試行後）: {FilePath}", maxRetries, jsonFilePath);
            throw new FileNotFoundException($"checklist.json が見つかりません（{maxRetries}回試行後）: {jsonFilePath}");
        }

        // ファイル読み込みも同様にリトライ
        string? json = null;
        for (int i = 1; i <= maxRetries; i++)
        {
            _logger.LogInformation("ファイル読み込み: {Attempt}/{MaxRetries}回目", i, maxRetries);

            try
            {
                var readTask = File.ReadAllTextAsync(jsonFilePath);
                json = await readTask.WaitAsync(TimeSpan.FromMilliseconds(retryIntervalMs)).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(json))
                {
                    _logger.LogInformation("ファイルを読み込みました（{Attempt}回目、{Length}文字）", i, json.Length);
                    break;
                }
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("ファイル読み込みがタイムアウトしました（{Attempt}/{MaxRetries}回目、{Interval}ms）",
                    i, maxRetries, retryIntervalMs);
            }

            // 最後の試行でない場合は待機
            if (i < maxRetries)
            {
                await Task.Delay(retryIntervalMs).ConfigureAwait(false);
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            _logger.LogError("ファイルの読み込みに失敗しました（{MaxRetries}回試行後）: {FilePath}", maxRetries, jsonFilePath);
            throw new IOException($"ファイルの読み込みに失敗しました（{maxRetries}回試行後）: {jsonFilePath}");
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
