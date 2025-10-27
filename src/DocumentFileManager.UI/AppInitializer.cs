using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DocumentFileManager.UI;

public static class AppInitializer
{
    public static IHost CreateHost(
        string documentRootPath,
        PathSettings? pathSettings = null,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        pathSettings ??= new PathSettings();

        EnsureProjectStructure(documentRootPath, pathSettings);

        var logsFolder = pathSettings.ToAbsolutePath(documentRootPath, pathSettings.LogsFolder);
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logsFolder, "app-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        configureLogger?.Invoke(loggerConfig);

        Log.Logger = loggerConfig.CreateLogger();
        Log.Information("Starting application host initialization.");
        Log.Information("Document root path: {DocumentRootPath}", documentRootPath);

        var host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(pathSettings);
                services.AddSingleton(_ => documentRootPath);

                services.AddDbContext<DocumentManagerContext>(options =>
                {
                    var dbPath = pathSettings.ToAbsolutePath(documentRootPath, pathSettings.DatabaseName);
                    Log.Information("Database path: {DbPath}", dbPath);
                    options.UseSqlite($"Data Source={dbPath}");
                });

                services.AddSingleton(new UISettings());

                services.AddScoped<ICheckItemRepository, CheckItemRepository>();
                services.AddScoped<IDocumentRepository, DocumentRepository>();
                services.AddScoped<ICheckItemDocumentRepository, CheckItemDocumentRepository>();

                services.AddScoped<IDataIntegrityService, DataIntegrityService>();
                services.AddSingleton<SettingsPersistence>();
                services.AddScoped<Infrastructure.Services.ChecklistLoader>();
                services.AddScoped<Infrastructure.Services.ChecklistSaver>();

                services.AddScoped<CheckItemUIBuilder>();

                services.AddTransient<MainWindow>();
                services.AddTransient<Windows.ChecklistWindow>();
                services.AddTransient<Windows.ChecklistEditorWindow>();
                services.AddTransient<Windows.SettingsWindow>();
                services.AddTransient<Windows.IntegrityReportWindow>();
            })
            .Build();

        Log.Information("Application host initialization completed.");
        return host;
    }

    public static async Task InitializeDatabaseAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();

        Log.Information("Ensuring database is migrated.");
        var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagerContext>();
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migration completed.");

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var pathSettings = scope.ServiceProvider.GetRequiredService<PathSettings>();
        var documentRoot = scope.ServiceProvider.GetRequiredService<string>();

        var candidates = new List<string>();

        void AddCandidate(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                candidates.Add(path);
            }
        }

        AddCandidate(pathSettings.ToAbsolutePath(documentRoot, pathSettings.SelectedChecklistFile));
        AddCandidate(pathSettings.ToAbsolutePath(documentRoot, pathSettings.ChecklistFile));

        if (!string.IsNullOrWhiteSpace(pathSettings.ChecklistDefinitionsFolder))
        {
            var definitionsRoot = pathSettings.ToAbsolutePath(documentRoot, pathSettings.ChecklistDefinitionsFolder);
            var selectedName = Path.GetFileName(pathSettings.SelectedChecklistFile);
            if (!string.IsNullOrEmpty(selectedName))
            {
                AddCandidate(Path.Combine(definitionsRoot, selectedName));
            }

            var fallbackName = Path.GetFileName(pathSettings.ChecklistFile);
            if (!string.IsNullOrEmpty(fallbackName))
            {
                AddCandidate(Path.Combine(definitionsRoot, fallbackName));
            }
        }

        string? checklistFullPath = null;
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                checklistFullPath = candidate;
                break;
            }
        }

        checklistFullPath ??= candidates.FirstOrDefault()
            ?? pathSettings.ToAbsolutePath(documentRoot, pathSettings.SelectedChecklistFile);

        Log.Information("Checklist path for seeding: {ChecklistPath}", checklistFullPath);

        var seeder = new DataSeeder(dbContext, loggerFactory, documentRoot, checklistFullPath);
        await seeder.SeedAsync();
    }

    public static void SetupGlobalExceptionHandlers(Application app)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                Log.Fatal(exception, "Unhandled exception observed in AppDomain.");
            }
        };

        app.DispatcherUnhandledException += (sender, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception observed on UI dispatcher.");
            MessageBox.Show(
                $"予期しないエラーが発生しました:\n{args.Exception.Message}\n\n詳細はログを確認してください。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        Log.Information("Global exception handlers registered.");
    }

    private static void EnsureProjectStructure(string documentRootPath, PathSettings pathSettings)
    {
        var directories = new[]
        {
            pathSettings.ToAbsolutePath(documentRootPath, pathSettings.LogsFolder),
            pathSettings.ToAbsolutePath(documentRootPath, pathSettings.ConfigDirectory),
            pathSettings.ToAbsolutePath(documentRootPath, pathSettings.DocumentsDirectory),
            pathSettings.ToAbsolutePath(documentRootPath, pathSettings.CapturesDirectory)
        };

        foreach (var directory in directories)
        {
            Directory.CreateDirectory(directory);
        }
    }
}
