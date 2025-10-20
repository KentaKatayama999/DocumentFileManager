using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using DocumentFileManager.UI.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI;

/// <summary>
/// UI設定ウィンドウ
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly UISettings _settings;
    private readonly PathSettings _pathSettings;
    private readonly ILogger<SettingsWindow> _logger;
    private readonly string _documentRootPath;

    public SettingsWindow(UISettings settings, PathSettings pathSettings, ILogger<SettingsWindow> logger, string documentRootPath)
    {
        _settings = settings;
        _pathSettings = pathSettings;
        _logger = logger;
        _documentRootPath = documentRootPath;

        InitializeComponent();

        // データバインディング設定
        DataContext = _settings;

        // 現在のチェックリストを表示
        CurrentChecklistText.Text = _pathSettings.SelectedChecklistFile;

        _logger.LogInformation("設定ウィンドウを開きました");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("UI設定の保存を開始します");

            // 入力値の検証
            if (!ValidateSettings(out string validationError))
            {
                _logger.LogWarning("入力値の検証に失敗しました: {Error}", validationError);
                MessageBox.Show(
                    $"入力値に問題があります:\n{validationError}",
                    "入力エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 設定ファイルパスを取得
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _pathSettings.SettingsFile);

            // 既存のJSONを読み込み
            string jsonContent = File.ReadAllText(settingsPath);
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // 新しいJSONを構築
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                // Logging セクションをコピー
                if (root.TryGetProperty("Logging", out var loggingElement))
                {
                    writer.WritePropertyName("Logging");
                    loggingElement.WriteTo(writer);
                }

                // PathSettings セクションを書き込み（更新された値を使用）
                writer.WritePropertyName("PathSettings");
                JsonSerializer.Serialize(writer, _pathSettings, options);

                // UISettings セクションを書き込み
                writer.WritePropertyName("UISettings");
                JsonSerializer.Serialize(writer, _settings, options);

                writer.WriteEndObject();
            }

            // ファイルに書き込み
            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(settingsPath, json);

            _logger.LogInformation("UI設定を保存しました: {Path}", settingsPath);

            MessageBox.Show(
                "設定を保存しました。\nアプリケーションを再起動すると変更が反映されます。",
                "設定保存",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UI設定の保存に失敗しました");
            MessageBox.Show(
                $"設定の保存に失敗しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("設定の変更をキャンセルしました");
        DialogResult = false;
        Close();
    }

    private void ChangeChecklistButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("チェックリスト変更ボタンがクリックされました");

            // チェックリスト選択ダイアログを表示
            var selectionDialog = new ChecklistSelectionDialog(_documentRootPath)
            {
                Owner = this
            };
            var dialogResult = selectionDialog.ShowDialog();

            if (dialogResult == true && !string.IsNullOrEmpty(selectionDialog.SelectedChecklistFileName))
            {
                // 選択されたチェックリストファイル名をPathSettingsに設定
                _pathSettings.SelectedChecklistFile = selectionDialog.SelectedChecklistFileName;
                _logger.LogInformation("チェックリストを変更しました: {FileName}", _pathSettings.SelectedChecklistFile);

                // 画面表示を更新
                CurrentChecklistText.Text = _pathSettings.SelectedChecklistFile;

                MessageBox.Show(
                    $"チェックリストを変更しました: {_pathSettings.SelectedChecklistFile}\n\n設定を保存してください。",
                    "チェックリスト変更",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックリスト変更中にエラーが発生しました");
            MessageBox.Show(
                $"チェックリストの変更に失敗しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 設定値の妥当性を検証する
    /// </summary>
    private bool ValidateSettings(out string errorMessage)
    {
        // チェックボックス設定の検証
        if (_settings.CheckBox.MinWidth <= 0)
        {
            errorMessage = "チェックボックスの最小幅は正の数値である必要があります";
            return false;
        }
        if (_settings.CheckBox.FontSize <= 0)
        {
            errorMessage = "チェックボックスのフォントサイズは正の数値である必要があります";
            return false;
        }
        if (_settings.CheckBox.MarginDepthMultiplier < 0)
        {
            errorMessage = "チェックボックスのマージン深さ倍率は0以上である必要があります";
            return false;
        }

        // グループボックス設定の検証
        if (_settings.GroupBox.RootMinWidth <= 0)
        {
            errorMessage = "グループボックスのルート最小幅は正の数値である必要があります";
            return false;
        }
        if (_settings.GroupBox.ChildItemMinWidth <= 0)
        {
            errorMessage = "グループボックスの子項目最小幅は正の数値である必要があります";
            return false;
        }
        if (_settings.GroupBox.ChildCategoryMinWidth <= 0)
        {
            errorMessage = "グループボックスの子分類最小幅は正の数値である必要があります";
            return false;
        }
        if (_settings.GroupBox.Padding < 0)
        {
            errorMessage = "グループボックスの内側の間隔は0以上である必要があります";
            return false;
        }
        if (_settings.GroupBox.BorderThickness < 0)
        {
            errorMessage = "グループボックスの枠線の太さは0以上である必要があります";
            return false;
        }

        // レイアウト設定の検証
        if (_settings.Layout.WrapPanelItemThreshold <= 0)
        {
            errorMessage = "項目横並び閾値は正の数値である必要があります";
            return false;
        }
        if (_settings.Layout.WrapPanelCategoryThreshold <= 0)
        {
            errorMessage = "分類横並び閾値は正の数値である必要があります";
            return false;
        }
        if (_settings.Layout.MaxColumnsPerRow <= 0)
        {
            errorMessage = "最大列数は正の数値である必要があります";
            return false;
        }
        if (_settings.Layout.WidthPerColumn <= 0)
        {
            errorMessage = "列あたり幅は正の数値である必要があります";
            return false;
        }
        if (_settings.Layout.GroupBoxExtraPadding < 0)
        {
            errorMessage = "追加間隔は0以上である必要があります";
            return false;
        }
        if (_settings.Layout.MaxCalculatedWidth <= 0)
        {
            errorMessage = "最大計算幅は正の数値である必要があります";
            return false;
        }

        // 色設定の検証（RGB値は0-255の範囲）
        if (!ValidateRgb(_settings.Colors.Depth0.R, _settings.Colors.Depth0.G, _settings.Colors.Depth0.B))
        {
            errorMessage = "大分類（深さ0）のRGB値は0～255の範囲である必要があります";
            return false;
        }
        if (!ValidateRgb(_settings.Colors.Depth1.R, _settings.Colors.Depth1.G, _settings.Colors.Depth1.B))
        {
            errorMessage = "中分類（深さ1）のRGB値は0～255の範囲である必要があります";
            return false;
        }
        if (!ValidateRgb(_settings.Colors.Depth2.R, _settings.Colors.Depth2.G, _settings.Colors.Depth2.B))
        {
            errorMessage = "小分類（深さ2）のRGB値は0～255の範囲である必要があります";
            return false;
        }
        if (!ValidateRgb(_settings.Colors.DepthDefault.R, _settings.Colors.DepthDefault.G, _settings.Colors.DepthDefault.B))
        {
            errorMessage = "デフォルト（深さ3以上）のRGB値は0～255の範囲である必要があります";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// RGB値の妥当性を検証する
    /// </summary>
    private bool ValidateRgb(int r, int g, int b)
    {
        return r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255;
    }
}
