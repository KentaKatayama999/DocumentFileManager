using System.IO;
using System.Text.Json;
using DocumentFileManager.UI.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// 設定の永続化サービス
/// </summary>
public class SettingsPersistence
{
    private readonly ILogger<SettingsPersistence> _logger;

    public SettingsPersistence(ILogger<SettingsPersistence> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// PathSettingsをappsettings.jsonに保存する
    /// </summary>
    public async Task SavePathSettingsAsync(PathSettings pathSettings, string appsettingsPath)
    {
        try
        {
            _logger.LogInformation("PathSettingsを保存します: {Path}", appsettingsPath);

            // 既存のappsettings.jsonを読み込む
            if (!File.Exists(appsettingsPath))
            {
                _logger.LogWarning("appsettings.jsonが見つかりません: {Path}", appsettingsPath);
                return;
            }

            var json = await File.ReadAllTextAsync(appsettingsPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // 新しいJSONオブジェクトを構築
            var newRoot = new Dictionary<string, object?>();

            // 既存のセクションをコピー
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name != "PathSettings")
                {
                    newRoot[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
                }
            }

            // PathSettingsセクションを更新
            newRoot["PathSettings"] = new Dictionary<string, object?>
            {
                { "LogsFolder", pathSettings.LogsFolder },
                { "DatabaseName", pathSettings.DatabaseName },
                { "ChecklistFile", pathSettings.ChecklistFile },
                { "SelectedChecklistFile", pathSettings.SelectedChecklistFile },
                { "SettingsFile", pathSettings.SettingsFile },
                { "CapturesDirectory", pathSettings.CapturesDirectory },
                { "DocumentsDirectory", pathSettings.DocumentsDirectory },
                { "ProjectRootLevelsUp", pathSettings.ProjectRootLevelsUp }
            };

            // JSONに書き込み
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var newJson = JsonSerializer.Serialize(newRoot, options);
            await File.WriteAllTextAsync(appsettingsPath, newJson);

            _logger.LogInformation("PathSettingsを保存しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PathSettingsの保存に失敗しました");
            throw;
        }
    }
}
