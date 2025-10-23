using System.IO;
using System.Windows;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Linq;

namespace DocumentFileManager.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        // コマンドライン引数からdocumentRootPathを取得
        var args = Environment.GetCommandLineArgs();
        string documentRootPath;

        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            // コマンドライン引数で指定された場合
            documentRootPath = Path.GetFullPath(args[1]);
        }
        else
        {
            // 引数がない場合はデフォルト（開発用：プロジェクトルートから5階層上）
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var pathSegments = Enumerable.Repeat("..", 5).ToArray();
            documentRootPath = Path.GetFullPath(Path.Combine(new[] { baseDirectory }.Concat(pathSegments).ToArray()));
        }

        // PathSettings を読み込み（appsettings.jsonから）
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)  // 個人設定（優先）
            .Build();

        var pathSettings = new PathSettings();
        configuration.GetSection("PathSettings").Bind(pathSettings);

        // AppInitializerを使用してホストを作成
        _host = AppInitializer.CreateHost(documentRootPath, pathSettings);

        // グローバル例外ハンドラを設定
        AppInitializer.SetupGlobalExceptionHandlers(this);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            Log.Information("アプリケーション起動処理を開始します");
            await _host.StartAsync();

            // AppInitializerを使用してデータベース初期化
            await AppInitializer.InitializeDatabaseAsync(_host);

            // MainWindow を DI コンテナから取得して表示
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow; // MainWindowプロパティに明示的に設定
            mainWindow.Show();

            Log.Information("メインウィンドウを表示しました");
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "アプリケーション起動時にエラーが発生しました");
            MessageBox.Show(
                $"アプリケーションの起動に失敗しました:\n{ex.Message}",
                "起動エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("アプリケーションを終了します");
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

