using DocumentFileManager.UI.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Windows;

namespace DocumentFileManager.UI;

/// <summary>
/// データ整合性チェック結果ウィンドウ
/// </summary>
public partial class IntegrityReportWindow : Window
{
    private readonly IDataIntegrityService _integrityService;
    private readonly ILogger<IntegrityReportWindow> _logger;
    private IntegrityReport? _report;

    public IntegrityReportWindow(
        IDataIntegrityService integrityService,
        ILogger<IntegrityReportWindow> logger)
    {
        InitializeComponent();
        _integrityService = integrityService;
        _logger = logger;

        Loaded += IntegrityReportWindow_Loaded;
    }

    private async void IntegrityReportWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("データ整合性チェックを開始します");

            // チェック実行
            _report = await _integrityService.CheckIntegrityAsync();

            // 結果表示
            DisplayReport(_report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "データ整合性チェックに失敗しました");
            MessageBox.Show(
                $"データ整合性チェックに失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }

    private void DisplayReport(IntegrityReport report)
    {
        var sb = new StringBuilder();

        // サマリー
        if (report.IsHealthy)
        {
            SummaryText.Text = "✓ データは正常です";
            SummaryText.Foreground = System.Windows.Media.Brushes.Green;
            DetailText.Text = "不整合は検出されませんでした。";
            RepairButton.IsEnabled = false;
        }
        else
        {
            SummaryText.Text = "⚠ 不整合が検出されました";
            SummaryText.Foreground = System.Windows.Media.Brushes.DarkOrange;

            int totalIssues = report.MissingFiles.Count + report.OrphanedCaptures.Count;
            DetailText.Text = $"合計 {totalIssues} 件の問題が見つかりました。";

            RepairButton.IsEnabled = true;
        }

        // 詳細ログ
        sb.AppendLine($"=== データ整合性チェック結果 ===");
        sb.AppendLine($"チェック日時: {report.CheckedAt:yyyy/MM/dd HH:mm:ss}");
        sb.AppendLine();

        // 存在しないファイル
        sb.AppendLine($"[存在しない資料ファイル]: {report.MissingFiles.Count} 件");
        if (report.MissingFiles.Count > 0)
        {
            foreach (var doc in report.MissingFiles)
            {
                sb.AppendLine($"  - ID:{doc.Id}, {doc.FileName} ({doc.RelativePath})");
            }
            sb.AppendLine();
        }

        // 孤立したキャプチャ
        sb.AppendLine($"[孤立したキャプチャ画像]: {report.OrphanedCaptures.Count} 件");
        if (report.OrphanedCaptures.Count > 0)
        {
            foreach (var capture in report.OrphanedCaptures)
            {
                sb.AppendLine($"  - {capture}");
            }
            sb.AppendLine();
        }

        LogTextBox.Text = sb.ToString();
        _logger.LogInformation("整合性チェック結果を表示しました: 健全={IsHealthy}, 問題数={IssueCount}",
            report.IsHealthy, report.MissingFiles.Count + report.OrphanedCaptures.Count);
    }

    private async void RepairButton_Click(object sender, RoutedEventArgs e)
    {
        if (_report == null || _report.IsHealthy)
        {
            return;
        }

        var result = MessageBox.Show(
            "データ修復を実行します。この操作は元に戻せません。\n\n続行しますか？",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            RepairButton.IsEnabled = false;
            _logger.LogInformation("データ修復を開始します");

            var options = new RepairOptions
            {
                RemoveMissingDocuments = DeleteMissingCheckBox.IsChecked == true,
                RemoveOrphanedCaptures = DeleteOrphanedCapturesCheckBox.IsChecked == true
            };

            await _integrityService.RepairIntegrityAsync(_report, options);

            MessageBox.Show(
                "データ修復が完了しました。",
                "完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _logger.LogInformation("データ修復が完了しました");

            // 再チェック
            _report = await _integrityService.CheckIntegrityAsync();
            DisplayReport(_report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "データ修復に失敗しました");
            MessageBox.Show(
                $"データ修復に失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            RepairButton.IsEnabled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
