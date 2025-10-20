using System.IO;
using System.Windows;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        // PathSettings を読み込み（早期に必要なため、Host.CreateDefaultBuilder前に読み込み）
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var pathSettings = new PathSettings();
        configuration.GetSection("PathSettings").Bind(pathSettings);

        // Serilog設定
        var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), pathSettings.LogsFolder);
        Directory.CreateDirectory(logsFolder);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logsFolder, "app-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("アプリケーションを起動しています...");

        // グローバル例外ハンドラ
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Log.Fatal(exception, "未処理の例外が発生しました");
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Error(args.Exception, "UI スレッドで未処理の例外が発生しました");
            MessageBox.Show(
                $"エラーが発生しました:\n{args.Exception.Message}\n\n詳細はログファイルを確認してください。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        _host = Host.CreateDefaultBuilder()
            .UseSerilog() // Serilog を Microsoft.Extensions.Logging に統合
            .ConfigureServices((context, services) =>
            {
                // PathSettings の登録
                var pathSettings = new PathSettings();
                context.Configuration.GetSection("PathSettings").Bind(pathSettings);
                services.AddSingleton(pathSettings);

                // ソリューションルートパスの設定（実行ファイルの場所から計算）
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var pathSegments = Enumerable.Repeat("..", pathSettings.ProjectRootLevelsUp).ToArray();
                var projectRoot = Path.GetFullPath(Path.Combine(new[] { baseDirectory }.Concat(pathSegments).ToArray()));

                // DbContext の登録（SQLite）
                services.AddDbContext<DocumentManagerContext>(options =>
                {
                    var dbPath = Path.Combine(projectRoot, pathSettings.DatabaseName);
                    Log.Information("DBパス: {DbPath}", dbPath);
                    options.UseSqlite($"Data Source={dbPath}");
                });

                // UI設定の登録
                var uiSettings = new UISettings();
                context.Configuration.GetSection("UISettings").Bind(uiSettings);
                services.AddSingleton(uiSettings);
                Log.Information("UI設定を読み込みました");

                // リポジトリの登録
                services.AddScoped<ICheckItemRepository, CheckItemRepository>();
                services.AddScoped<IDocumentRepository, DocumentRepository>();
                services.AddScoped<ICheckItemDocumentRepository, CheckItemDocumentRepository>();

                // サービスの登録
                services.AddScoped<IDataIntegrityService, DataIntegrityService>();
                services.AddSingleton<SettingsPersistence>();

                // UIヘルパーの登録
                services.AddScoped<CheckItemUIBuilder>();

                // ウィンドウの登録
                services.AddTransient<MainWindow>();
            })
            .Build();

        Log.Information("アプリケーションの初期化が完了しました");
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            Log.Information("アプリケーション起動処理を開始します");
            await _host.StartAsync();

            // データベースマイグレーション自動適用（デフォルトのチェックリストファイルを使用）
            using (var scope = _host.Services.CreateScope())
            {
                Log.Information("データベースマイグレーションを確認しています...");
                var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagerContext>();
                await dbContext.Database.MigrateAsync();
                Log.Information("データベースマイグレーションが完了しました");

                // シードデータ投入（デフォルトのチェックリストファイルを使用）
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var pathSettings = scope.ServiceProvider.GetRequiredService<PathSettings>();
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var pathSegments = Enumerable.Repeat("..", pathSettings.ProjectRootLevelsUp).ToArray();
                var projectRoot = Path.GetFullPath(Path.Combine(new[] { baseDirectory }.Concat(pathSegments).ToArray()));
                Log.Information("シードデータ投入用プロジェクトルート: {ProjectRoot}", projectRoot);
                var seeder = new DataSeeder(dbContext, loggerFactory, projectRoot, pathSettings.SelectedChecklistFile);
                await seeder.SeedAsync();
            }

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

