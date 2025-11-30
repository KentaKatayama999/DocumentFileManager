using System.IO;
using System.Windows;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Factories;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.Services.Abstractions;
using DocumentFileManager.UI.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DocumentFileManager.UI;

/// <summary>
/// アプリケーション初期化クラス
/// DIコンテナの構築とサービス登録を行う
/// </summary>
public static class AppInitializer
{
    /// <summary>
    /// アプリケーションを初期化してIHostを作成
    /// </summary>
    /// <param name="documentRootPath">プロジェクトのルートパス</param>
    /// <param name="pathSettings">パス設定（nullの場合はデフォルト値を使用）</param>
    /// <param name="configureLogger">Serilog設定のカスタマイズ（オプション）</param>
    /// <returns>初期化されたIHost</returns>
    public static IHost CreateHost(
        string documentRootPath,
        PathSettings? pathSettings = null,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        // デフォルトのPathSettingsを使用
        pathSettings ??= new PathSettings();

        // Serilog設定（documentRootPath配下のLogsフォルダに出力）
        var logsFolder = Path.Combine(documentRootPath, pathSettings.LogsFolder);
        Directory.CreateDirectory(logsFolder);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logsFolder, "app-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        // カスタム設定があれば適用
        configureLogger?.Invoke(loggerConfig);

        Log.Logger = loggerConfig.CreateLogger();
        Log.Information("アプリケーションを初期化しています...");
        Log.Information("documentRootPath: {DocumentRootPath}", documentRootPath);

        var host = Host.CreateDefaultBuilder()
            .UseSerilog() // Serilog を Microsoft.Extensions.Logging に統合
            .ConfigureServices((context, services) =>
            {
                // PathSettings の登録
                services.AddSingleton(pathSettings);

                // documentRootPath をサービスとして登録（依存性注入で使用）
                services.AddSingleton(_ => documentRootPath);

                // DbContext の登録（SQLite）
                services.AddDbContext<DocumentManagerContext>(options =>
                {
                    var dbPath = Path.Combine(documentRootPath, pathSettings.DatabaseName);
                    Log.Information("DBパス: {DbPath}", dbPath);

                    // パフォーマンス最適化された接続文字列
                    // Cache=Shared: 複数接続でキャッシュ共有
                    var connectionString = $"Data Source={dbPath};Cache=Shared";
                    Log.Information("SQLite接続文字列: Cache=Shared");
                    options.UseSqlite(connectionString);
                });

                // UI設定の登録（デフォルト値を使用）
                var uiSettings = new UISettings();
                services.AddSingleton(uiSettings);
                Log.Information("UI設定を読み込みました");

                // リポジトリの登録
                services.AddScoped<ICheckItemRepository, CheckItemRepository>();
                services.AddScoped<IDocumentRepository, DocumentRepository>();
                services.AddScoped<ICheckItemDocumentRepository, CheckItemDocumentRepository>();

                // サービスの登録
                services.AddSingleton<IDialogService, WpfDialogService>();
                services.AddScoped<IChecklistStateManager, ChecklistStateManager>();
                services.AddScoped<IDataIntegrityService, DataIntegrityService>();
                services.AddSingleton<SettingsPersistence>();
                services.AddScoped<Infrastructure.Services.ChecklistLoader>();
                services.AddScoped<Infrastructure.Services.ChecklistSaver>();
                services.AddScoped<IDocumentService, DocumentService>();
                services.AddScoped<IChecklistService, ChecklistService>();

                // ファクトリの登録
                services.AddScoped<ICheckItemViewModelFactory>(sp =>
                {
                    var docRoot = sp.GetRequiredService<string>();
                    var logger = sp.GetRequiredService<ILogger<CheckItemViewModelFactory>>();
                    return new CheckItemViewModelFactory(docRoot, logger);
                });

                // UIヘルパーの登録
                services.AddScoped<CheckItemUIBuilder>();

                // ウィンドウの登録
                services.AddTransient<MainWindow>();
                services.AddTransient<Windows.ChecklistWindow>();
                services.AddTransient<Windows.ChecklistEditorWindow>();
                services.AddTransient<Windows.SettingsWindow>();
                services.AddTransient<Windows.IntegrityReportWindow>();
            })
            .Build();

        Log.Information("アプリケーションの初期化が完了しました");
        return host;
    }

    /// <summary>
    /// データベースマイグレーションとシードデータ投入を実行
    /// </summary>
    /// <param name="host">IHost</param>
    /// <returns></returns>
    public static async Task InitializeDatabaseAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();

        Log.Information("データベースマイグレーションを確認しています...");
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagerContext>();
        await dbContext.Database.MigrateAsync();
        Log.Information("データベースマイグレーションが完了しました");

        // シードデータ投入
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var pathSettings = scope.ServiceProvider.GetRequiredService<PathSettings>();
        var documentRoot = scope.ServiceProvider.GetRequiredService<string>();
        Log.Information("シードデータ投入用documentRoot: {DocumentRoot}", documentRoot);

        var seeder = new DataSeeder(dbContext, loggerFactory, documentRoot, pathSettings.SelectedChecklistFile);
        await seeder.SeedAsync();
    }

    /// <summary>
    /// グローバル例外ハンドラを設定（WPF Application用）
    /// </summary>
    /// <param name="app">Applicationインスタンス</param>
    public static void SetupGlobalExceptionHandlers(Application app)
    {
        // AppDomainレベルの例外ハンドラ
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Log.Fatal(exception, "未処理の例外が発生しました");
        };

        // WPF UIスレッドの例外ハンドラ
        app.DispatcherUnhandledException += (sender, args) =>
        {
            Log.Error(args.Exception, "UI スレッドで未処理の例外が発生しました");
            MessageBox.Show(
                $"エラーが発生しました:\n{args.Exception.Message}\n\n詳細はログファイルを確認してください。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        Log.Information("グローバル例外ハンドラを設定しました");
    }
}
