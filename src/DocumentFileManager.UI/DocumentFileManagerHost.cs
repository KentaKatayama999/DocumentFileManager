using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Dialogs;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Serilog;

namespace DocumentFileManager.UI;

public class DocumentFileManagerHost : IDisposable
{
    private IHost? _host;
    private bool _disposed;

    public IServiceProvider? ServiceProvider => _host?.Services;
    public bool IsInitialized => _host != null;

    #region Entry Points

    public static void ShowMainWindow(string documentRootPath) =>
        ShowMainWindow(documentRootPath, null);

    public static void ShowMainWindow(string documentRootPath, PathSettings? pathSettings)
    {
        var resolvedSettings = ResolvePathSettings(documentRootPath, pathSettings);
        EnsureChecklistDefinitionsFolder(documentRootPath, resolvedSettings);

        var checklistPath = EnsureChecklistAvailable(documentRootPath, resolvedSettings);
        if (string.IsNullOrEmpty(checklistPath) || !File.Exists(checklistPath))
        {
            checklistPath = PromptForChecklist(documentRootPath, resolvedSettings);
            if (string.IsNullOrEmpty(checklistPath))
            {
                Log.Warning("Checklist file was not selected. Application start cancelled.");
                MessageBox.Show(
                    "チェックリストファイルが選択されなかったため、アプリケーションを終了します。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
        }

        SavePathSettingsToAppSettings(documentRootPath, resolvedSettings);

        var host = new DocumentFileManagerHost();
        host.Initialize(documentRootPath, resolvedSettings);
        host.InitializeDatabaseAsync().GetAwaiter().GetResult();

        var mainWindow = host.CreateMainWindow();
        mainWindow.ShowDialog();

        host.Dispose();
    }

    public static void ShowChecklistEditor(string documentRootPath) =>
        ShowChecklistEditor(documentRootPath, null);

    public static void ShowChecklistEditor(string documentRootPath, PathSettings? pathSettings)
    {
        var resolvedSettings = ResolvePathSettings(documentRootPath, pathSettings);
        EnsureChecklistDefinitionsFolder(documentRootPath, resolvedSettings);
        var checklistPath = EnsureChecklistAvailable(documentRootPath, resolvedSettings);
        if (string.IsNullOrEmpty(checklistPath) || !File.Exists(checklistPath))
        {
            checklistPath = PromptForChecklist(documentRootPath, resolvedSettings);
            if (string.IsNullOrEmpty(checklistPath))
            {
                Log.Warning("Checklist file was not selected. Checklist editor will not open.");
                MessageBox.Show(
                    "チェックリストファイルが選択されなかったため、エディタを開けません。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
        }

        SavePathSettingsToAppSettings(documentRootPath, resolvedSettings);

        var host = new DocumentFileManagerHost();
        host.Initialize(documentRootPath, resolvedSettings);

        var editorWindow = host.CreateChecklistEditorWindow();
        editorWindow.ShowDialog();

        host.Dispose();
    }

    #endregion

    #region Host Lifetime

    public DocumentFileManagerHost Initialize(
        string documentRootPath,
        PathSettings? pathSettings = null,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        if (_host != null)
        {
            throw new InvalidOperationException("Host has already been initialized.");
        }

        if (string.IsNullOrWhiteSpace(documentRootPath))
        {
            throw new ArgumentException("documentRootPath is required.", nameof(documentRootPath));
        }

        if (!Directory.Exists(documentRootPath))
        {
            throw new DirectoryNotFoundException($"documentRootPath was not found: {documentRootPath}");
        }

        _host = AppInitializer.CreateHost(documentRootPath, pathSettings, configureLogger);
        _host.Start();

        return this;
    }

    public async Task<DocumentFileManagerHost> InitializeDatabaseAsync()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize() must be called before InitializeDatabaseAsync().");
        }

        await AppInitializer.InitializeDatabaseAsync(_host);
        return this;
    }

    public MainWindow CreateMainWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize() must be called before creating windows.");
        }

        return _host.Services.GetRequiredService<MainWindow>();
    }

    public ChecklistWindow CreateChecklistWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize() must be called before creating windows.");
        }

        return _host.Services.GetRequiredService<ChecklistWindow>();
    }

    public ChecklistEditorWindow CreateChecklistEditorWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize() must be called before creating windows.");
        }

        return _host.Services.GetRequiredService<ChecklistEditorWindow>();
    }

    public SettingsWindow CreateSettingsWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize() must be called before creating windows.");
        }

        return _host.Services.GetRequiredService<SettingsWindow>();
    }

    public IntegrityReportWindow CreateIntegrityReportWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize() must be called before creating windows.");
        }

        return _host.Services.GetRequiredService<IntegrityReportWindow>();
    }

    #endregion

    #region Helpers

    private static PathSettings ResolvePathSettings(string documentRootPath, PathSettings? pathSettings)
    {
        var appsettingsPath = Path.Combine(documentRootPath, "appsettings.json");
        var loaded = SettingsPersistence.LoadPathSettingsFromFile(appsettingsPath);
        var resolved = loaded ?? pathSettings ?? new PathSettings();
        Directory.CreateDirectory(resolved.ToAbsolutePath(documentRootPath, resolved.ConfigDirectory));
        return resolved;
    }

    private static void EnsureChecklistDefinitionsFolder(string documentRootPath, PathSettings pathSettings)
    {
        if (!string.IsNullOrWhiteSpace(pathSettings.ChecklistDefinitionsFolder))
        {
            return;
        }

        Log.Information("Checklist definitions folder is not set. Prompting user.");
        var selectedFolder = SelectChecklistDefinitionsFolder(documentRootPath);

        if (!string.IsNullOrEmpty(selectedFolder))
        {
            pathSettings.ChecklistDefinitionsFolder = selectedFolder;
            Log.Information("Checklist definitions folder selected: {Folder}", selectedFolder);
        }
        else
        {
            pathSettings.ChecklistDefinitionsFolder = documentRootPath;
            Log.Information("Folder selection cancelled. Falling back to document root: {Path}", documentRootPath);
        }
    }

    private static string EnsureChecklistAvailable(string documentRootPath, PathSettings pathSettings)
    {
        var configFolder = pathSettings.ToAbsolutePath(documentRootPath, pathSettings.ConfigDirectory);
        Directory.CreateDirectory(configFolder);

        foreach (var candidate in EnumerateChecklistCandidates(documentRootPath, pathSettings))
        {
            if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
            {
                continue;
            }

            return CopyChecklistToConfigIfNeeded(documentRootPath, pathSettings, candidate, configFolder);
        }

        return string.Empty;
    }

    private static IEnumerable<string> EnumerateChecklistCandidates(string documentRootPath, PathSettings pathSettings)
    {
        if (!string.IsNullOrWhiteSpace(pathSettings.SelectedChecklistFile))
        {
            yield return pathSettings.ToAbsolutePath(documentRootPath, pathSettings.SelectedChecklistFile);
        }

        if (!string.IsNullOrWhiteSpace(pathSettings.ChecklistFile))
        {
            yield return pathSettings.ToAbsolutePath(documentRootPath, pathSettings.ChecklistFile);
        }

        if (!string.IsNullOrWhiteSpace(pathSettings.ChecklistDefinitionsFolder))
        {
            var definitionsRoot = pathSettings.ToAbsolutePath(documentRootPath, pathSettings.ChecklistDefinitionsFolder);
            var selectedName = Path.GetFileName(pathSettings.SelectedChecklistFile);
            var checklistName = Path.GetFileName(pathSettings.ChecklistFile);

            if (!string.IsNullOrEmpty(selectedName))
            {
                yield return Path.Combine(definitionsRoot, selectedName);
            }

            if (!string.IsNullOrEmpty(checklistName) &&
                !string.Equals(checklistName, selectedName, StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(definitionsRoot, checklistName);
            }
        }
    }

    private static string PromptForChecklist(string documentRootPath, PathSettings pathSettings)
    {
        var dialog = new ChecklistSelectionDialog(documentRootPath, pathSettings);
        var dialogResult = dialog.ShowDialog();

        if (dialogResult == true && !string.IsNullOrEmpty(dialog.SelectedChecklistFilePath))
        {
            var configFolder = pathSettings.ToAbsolutePath(documentRootPath, pathSettings.ConfigDirectory);
            var copiedPath = CopyChecklistToConfigIfNeeded(
                documentRootPath,
                pathSettings,
                dialog.SelectedChecklistFilePath,
                configFolder);

            var sourceFolder = Path.GetDirectoryName(dialog.SelectedChecklistFilePath);
            if (!string.IsNullOrEmpty(sourceFolder))
            {
                pathSettings.ChecklistDefinitionsFolder = sourceFolder;
            }

            Log.Information("Checklist copied locally: {Source} -> {Destination}", dialog.SelectedChecklistFilePath, copiedPath);
            return copiedPath;
        }

        return string.Empty;
    }

    private static string CopyChecklistToConfigIfNeeded(
        string documentRootPath,
        PathSettings pathSettings,
        string sourcePath,
        string configFolder)
    {
        var absoluteSource = Path.GetFullPath(sourcePath);
        Directory.CreateDirectory(configFolder);

        if (IsPathWithinDirectory(absoluteSource, configFolder))
        {
            SetChecklistSelection(documentRootPath, pathSettings, absoluteSource);
            return absoluteSource;
        }

        var destinationPath = Path.Combine(configFolder, Path.GetFileName(absoluteSource));
        File.Copy(absoluteSource, destinationPath, overwrite: true);
        SetChecklistSelection(documentRootPath, pathSettings, destinationPath);

        var sourceFolder = Path.GetDirectoryName(absoluteSource);
        if (!string.IsNullOrEmpty(sourceFolder))
        {
            pathSettings.ChecklistDefinitionsFolder = sourceFolder;
        }

        return destinationPath;
    }

    private static void SetChecklistSelection(string documentRootPath, PathSettings pathSettings, string absolutePath)
    {
        var relativePath = Path.GetRelativePath(documentRootPath, absolutePath);
        pathSettings.SelectedChecklistFile = relativePath;
        pathSettings.ChecklistFile = relativePath;
    }

    private static bool IsPathWithinDirectory(string path, string directory)
    {
        var normalizedPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedPath.StartsWith(
            normalizedDirectory + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? SelectChecklistDefinitionsFolder(string documentRootPath)
    {
        var dialog = new OpenFileDialog
        {
            Title = "チェックリスト定義フォルダを選択",
            FileName = "folder.selector",
            Filter = "フォルダ|*.none",
            CheckFileExists = false,
            CheckPathExists = true,
            InitialDirectory = documentRootPath
        };

        if (dialog.ShowDialog() == true)
        {
            var selectedFolder = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(selectedFolder) && Directory.Exists(selectedFolder))
            {
                return selectedFolder;
            }
        }

        return null;
    }

    private static void SavePathSettingsToAppSettings(string documentRootPath, PathSettings pathSettings)
    {
        try
        {
            var settingsPath = Path.Combine(documentRootPath, pathSettings.SettingsFile);
            var options = new JsonWriterOptions { Indented = true };

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();

                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    using var document = JsonDocument.Parse(jsonContent);
                    var root = document.RootElement;

                    if (root.TryGetProperty("Logging", out var loggingElement))
                    {
                        writer.WritePropertyName("Logging");
                        loggingElement.WriteTo(writer);
                    }

                    writer.WritePropertyName("PathSettings");
                    JsonSerializer.Serialize(writer, pathSettings, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    if (root.TryGetProperty("UISettings", out var uiSettingsElement))
                    {
                        writer.WritePropertyName("UISettings");
                        uiSettingsElement.WriteTo(writer);
                    }
                }
                else
                {
                    writer.WritePropertyName("PathSettings");
                    JsonSerializer.Serialize(writer, pathSettings, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    Log.Information("Created new appsettings.json at {Path}", settingsPath);
                }

                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(settingsPath, json);
            Log.Information("PathSettings persisted to {Path}", settingsPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist PathSettings.");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _host?.StopAsync().Wait();
            _host?.Dispose();
            Log.CloseAndFlush();
        }

        _disposed = true;
    }

    #endregion
}
