using System.Windows;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ICheckItemRepository _checkItemRepository;
    private readonly CheckItemUIBuilder _checkItemUIBuilder;
    private readonly UISettings _uiSettings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(
        IDocumentRepository documentRepository,
        ICheckItemRepository checkItemRepository,
        CheckItemUIBuilder checkItemUIBuilder,
        UISettings uiSettings,
        IServiceProvider serviceProvider,
        ILogger<MainWindow> logger)
    {
        _documentRepository = documentRepository;
        _checkItemRepository = checkItemRepository;
        _checkItemUIBuilder = checkItemUIBuilder;
        _uiSettings = uiSettings;
        _serviceProvider = serviceProvider;
        _logger = logger;

        InitializeComponent();

        // ウィンドウが閉じられたときにアプリケーションを確実にシャットダウン
        Closed += (s, e) => Application.Current.Shutdown();

        _logger.LogInformation("MainWindow が初期化されました");
    }

    private async void LoadDocumentsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("資料の読み込みを開始します");
            StatusText.Text = "資料を読み込み中...";

            var documents = await _documentRepository.GetAllAsync();
            DocumentsListView.ItemsSource = documents;
            DocumentCountText.Text = $"{documents.Count} 件";

            _logger.LogInformation("資料を {Count} 件読み込みました", documents.Count);
            StatusText.Text = "資料の読み込みが完了しました";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料の読み込みに失敗しました");
            StatusText.Text = $"エラー: {ex.Message}";
            MessageBox.Show($"資料の読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadCheckItemsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("チェック項目の読み込みを開始します");
            StatusText.Text = "チェック項目を読み込み中...";

            // UIBuilderを使用してGroupBox階層を構築
            await _checkItemUIBuilder.BuildAsync(CheckItemsContainer);

            // ルート項目の数を取得して表示
            var rootItems = await _checkItemRepository.GetRootItemsAsync();
            CheckItemCountText.Text = $"{rootItems.Count} 件（ルート項目）";

            _logger.LogInformation("チェック項目の階層表示が完了しました（ルート項目: {Count} 件）", rootItems.Count);
            StatusText.Text = "チェック項目の読み込みが完了しました";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック項目の読み込みに失敗しました");
            StatusText.Text = $"エラー: {ex.Message}";
            MessageBox.Show($"チェック項目の読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("設定ウィンドウを開きます");

            // SettingsWindowをDIコンテナから取得
            var pathSettings = _serviceProvider.GetRequiredService<PathSettings>();
            var settingsLogger = _serviceProvider.GetRequiredService<ILogger<SettingsWindow>>();
            var settingsWindow = new SettingsWindow(_uiSettings, pathSettings, settingsLogger)
            {
                Owner = this
            };

            var result = settingsWindow.ShowDialog();

            if (result == true)
            {
                _logger.LogInformation("設定が保存されました");
                StatusText.Text = "設定を保存しました（再起動後に反映されます）";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ウィンドウの表示に失敗しました");
            MessageBox.Show($"設定ウィンドウの表示に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("アプリケーションを終了します");
        Close();
    }
}