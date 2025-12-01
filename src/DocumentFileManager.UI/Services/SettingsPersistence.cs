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
    /// PathSettingsをappsettings.local.jsonに保存する
    /// </summary>
    /// <remarks>
    /// appsettings.local.jsonはビルド時に上書きされないユーザー設定ファイルとして使用。
    /// App.xaml.csで読み込み時に優先されるため、ユーザー設定は正しく反映される。
    /// </remarks>
    public async Task SavePathSettingsAsync(PathSettings pathSettings, string appsettingsPath)
    {
        try
        {
            // appsettings.local.json に保存（ビルドで上書きされない）
            var localSettingsPath = Path.Combine(Path.GetDirectoryName(appsettingsPath)!, "appsettings.local.json");
            _logger.LogInformation("PathSettingsを保存します: {Path}", localSettingsPath);

            // 既存のappsettings.local.jsonを読み込む（なければ空のオブジェクト）
            var newRoot = new Dictionary<string, object?>();
            if (File.Exists(localSettingsPath))
            {
                var json = await File.ReadAllTextAsync(localSettingsPath);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // 既存のセクションをコピー
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name != "PathSettings")
                    {
                        newRoot[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
                    }
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
                { "CapturesDirectory", pathSettings.CapturesDirectory }
            };

            // JSONに書き込み
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var newJson = JsonSerializer.Serialize(newRoot, options);
            await File.WriteAllTextAsync(localSettingsPath, newJson);

            _logger.LogInformation("PathSettingsを appsettings.local.json に保存しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PathSettingsの保存に失敗しました");
            throw;
        }
    }
}
