using System.IO;
using System.Windows;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DocumentFileManager.UI;

/// <summary>
/// DocumentFileManagerの公開APIクラス
/// 他のアプリケーションからライブラリとして使用する際のエントリーポイント
/// </summary>
public class DocumentFileManagerHost : IDisposable
{
    private IHost? _host;
    private bool _disposed;

    /// <summary>
    /// DIコンテナのサービスプロバイダー
    /// </summary>
    public IServiceProvider? ServiceProvider => _host?.Services;

    /// <summary>
    /// ホストが初期化済みかどうか
    /// </summary>
    public bool IsInitialized => _host != null;

    #region 静的メソッド（シンプルな使用方法）

    /// <summary>
    /// MainWindowを表示（最もシンプルな使用方法）
    /// </summary>
    /// <param name="documentRootPath">プロジェクトのルートパス</param>
    public static void ShowMainWindow(string documentRootPath)
    {
        ShowMainWindow(documentRootPath, null);
    }

    /// <summary>
    /// MainWindowを表示（カスタム設定を指定）
    /// </summary>
    /// <param name="documentRootPath">プロジェクトのルートパス</param>
    /// <param name="pathSettings">パス設定（nullの場合はデフォルト）</param>
    public static void ShowMainWindow(string documentRootPath, PathSettings? pathSettings)
    {
        // pathSettings が null の場合、新しい PathSettings を作成
        pathSettings ??= new PathSettings();

        // ChecklistDefinitionsFolder が未設定の場合、フォルダ選択ダイアログを表示
        if (string.IsNullOrEmpty(pathSettings.ChecklistDefinitionsFolder))
        {
            Log.Information("チェックリスト定義フォルダが未設定のため、フォルダ選択ダイアログを表示します");

            var selectedFolder = SelectChecklistDefinitionsFolder(documentRootPath);

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                pathSettings.ChecklistDefinitionsFolder = selectedFolder;
                Log.Information("チェックリスト定義フォルダが選択されました: {Folder}", selectedFolder);

                // PathSettings を appsettings.json に保存（永続化）
                SavePathSettingsToAppSettings(documentRootPath, pathSettings);
            }
            else
            {
                // キャンセルされた場合はドキュメントルートをデフォルトとして使用
                pathSettings.ChecklistDefinitionsFolder = documentRootPath;
                Log.Information("フォルダ選択がキャンセルされたため、documentRootPath を使用します: {Path}", documentRootPath);
            }
        }

        var host = new DocumentFileManagerHost();
        host.Initialize(documentRootPath, pathSettings);
        host.InitializeDatabaseAsync().Wait();

        var mainWindow = host.CreateMainWindow();
        mainWindow.ShowDialog(); // モーダル表示

        host.Dispose();
    }

    /// <summary>
    /// チェックリストエディターウィンドウを表示
    /// </summary>
    /// <param name="documentRootPath">プロジェクトのルートパス</param>
    public static void ShowChecklistEditor(string documentRootPath)
    {
        ShowChecklistEditor(documentRootPath, null);
    }

    /// <summary>
    /// チェックリストエディターウィンドウを表示（カスタム設定を指定）
    /// </summary>
    /// <param name="documentRootPath">プロジェクトのルートパス</param>
    /// <param name="pathSettings">パス設定（nullの場合はデフォルト）</param>
    public static void ShowChecklistEditor(string documentRootPath, PathSettings? pathSettings)
    {
        var host = new DocumentFileManagerHost();
        host.Initialize(documentRootPath, pathSettings);

        var editorWindow = host.CreateChecklistEditorWindow();
        editorWindow.ShowDialog();

        host.Dispose();
    }

    #endregion

    #region インスタンスメソッド（詳細な制御が必要な場合）

    /// <summary>
    /// アプリケーションを初期化
    /// </summary>
    /// <param name="documentRootPath">プロジェクトのルートパス</param>
    /// <param name="pathSettings">パス設定（nullの場合はデフォルト）</param>
    /// <param name="configureLogger">Serilog設定のカスタマイズ（オプション）</param>
    /// <returns>このインスタンス（メソッドチェーン可能）</returns>
    public DocumentFileManagerHost Initialize(
        string documentRootPath,
        PathSettings? pathSettings = null,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        if (_host != null)
        {
            throw new InvalidOperationException("既に初期化されています。");
        }

        if (string.IsNullOrWhiteSpace(documentRootPath))
        {
            throw new ArgumentException("documentRootPathは必須です。", nameof(documentRootPath));
        }

        if (!Directory.Exists(documentRootPath))
        {
            throw new DirectoryNotFoundException($"指定されたパスが見つかりません: {documentRootPath}");
        }

        _host = AppInitializer.CreateHost(documentRootPath, pathSettings, configureLogger);
        _host.Start();

        return this;
    }

    /// <summary>
    /// データベースマイグレーションとシードデータ投入を実行
    /// </summary>
    /// <returns></returns>
    public async Task<DocumentFileManagerHost> InitializeDatabaseAsync()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize()を先に呼び出してください。");
        }

        await AppInitializer.InitializeDatabaseAsync(_host);
        return this;
    }

    /// <summary>
    /// MainWindowを作成
    /// </summary>
    /// <returns>MainWindowインスタンス</returns>
    public MainWindow CreateMainWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize()を先に呼び出してください。");
        }

        return _host.Services.GetRequiredService<MainWindow>();
    }

    /// <summary>
    /// ChecklistWindowを作成
    /// </summary>
    /// <returns>ChecklistWindowインスタンス</returns>
    public ChecklistWindow CreateChecklistWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize()を先に呼び出してください。");
        }

        return _host.Services.GetRequiredService<ChecklistWindow>();
    }

    /// <summary>
    /// ChecklistEditorWindowを作成
    /// </summary>
    /// <returns>ChecklistEditorWindowインスタンス</returns>
    public ChecklistEditorWindow CreateChecklistEditorWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize()を先に呼び出してください。");
        }

        return _host.Services.GetRequiredService<ChecklistEditorWindow>();
    }

    /// <summary>
    /// SettingsWindowを作成
    /// </summary>
    /// <returns>SettingsWindowインスタンス</returns>
    public SettingsWindow CreateSettingsWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize()を先に呼び出してください。");
        }

        return _host.Services.GetRequiredService<SettingsWindow>();
    }

    /// <summary>
    /// IntegrityReportWindowを作成
    /// </summary>
    /// <returns>IntegrityReportWindowインスタンス</returns>
    public IntegrityReportWindow CreateIntegrityReportWindow()
    {
        if (_host == null)
        {
            throw new InvalidOperationException("Initialize()を先に呼び出してください。");
        }

        return _host.Services.GetRequiredService<IntegrityReportWindow>();
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// チェックリスト定義フォルダを選択
    /// </summary>
    /// <param name="documentRootPath">デフォルトのドキュメントルートパス</param>
    /// <returns>選択されたフォルダパス（キャンセル時は null）</returns>
    private static string? SelectChecklistDefinitionsFolder(string documentRootPath)
    {
        // OpenFileDialogをフォルダ選択モードで使用
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "チェックリスト定義ファイルの保存先フォルダを選択",
            FileName = "フォルダ選択",
            Filter = "フォルダ|*.none",
            CheckFileExists = false,
            CheckPathExists = true,
            InitialDirectory = documentRootPath
        };

        if (dialog.ShowDialog() == true)
        {
            // 選択されたファイルのディレクトリを取得
            var selectedFolder = Path.GetDirectoryName(dialog.FileName);

            if (!string.IsNullOrEmpty(selectedFolder) && Directory.Exists(selectedFolder))
            {
                return selectedFolder;
            }
        }

        return null;
    }

    /// <summary>
    /// PathSettings を appsettings.json に保存
    /// </summary>
    /// <param name="documentRootPath">ドキュメントルートパス</param>
    /// <param name="pathSettings">保存する PathSettings</param>
    private static void SavePathSettingsToAppSettings(string documentRootPath, PathSettings pathSettings)
    {
        try
        {
            // 設定ファイルパスを取得
            var settingsPath = Path.Combine(documentRootPath, pathSettings.SettingsFile);

            // 新しいJSONを構築
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using var stream = new MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                // ファイルが存在する場合は既存のセクションをコピー
                if (File.Exists(settingsPath))
                {
                    string jsonContent = File.ReadAllText(settingsPath);
                    using var document = System.Text.Json.JsonDocument.Parse(jsonContent);
                    var root = document.RootElement;

                    // Logging セクションをコピー
                    if (root.TryGetProperty("Logging", out var loggingElement))
                    {
                        writer.WritePropertyName("Logging");
                        loggingElement.WriteTo(writer);
                    }

                    // PathSettings セクションを書き込み（更新された値を使用）
                    writer.WritePropertyName("PathSettings");
                    System.Text.Json.JsonSerializer.Serialize(writer, pathSettings, options);

                    // UISettings セクションをコピー
                    if (root.TryGetProperty("UISettings", out var uiSettingsElement))
                    {
                        writer.WritePropertyName("UISettings");
                        uiSettingsElement.WriteTo(writer);
                    }
                }
                else
                {
                    // ファイルが存在しない場合は新規作成（PathSettingsのみ）
                    writer.WritePropertyName("PathSettings");
                    System.Text.Json.JsonSerializer.Serialize(writer, pathSettings, options);

                    Log.Information("appsettings.json が存在しないため、新規作成します: {Path}", settingsPath);
                }

                writer.WriteEndObject();
            }

            // ファイルに書き込み
            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(settingsPath, json);

            Log.Information("PathSettings を appsettings.json に保存しました: {Path}", settingsPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PathSettings の保存に失敗しました");
        }
    }

    #endregion

    #region IDisposable実装

    /// <summary>
    /// リソースを解放
    /// </summary>
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
