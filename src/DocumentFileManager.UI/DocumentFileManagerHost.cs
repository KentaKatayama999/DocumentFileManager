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
        var host = new DocumentFileManagerHost();
        host.Initialize(documentRootPath, pathSettings);
        host.InitializeDatabaseAsync().Wait();

        var mainWindow = host.CreateMainWindow();

        // Closedイベントでのみ終了するようにする（Hide時に終了しないように）
        var closed = false;
        mainWindow.Closed += (s, e) => closed = true;

        mainWindow.Show();

        // メッセージループを手動で回す（MainWindowが閉じられるまで）
        var frame = new System.Windows.Threading.DispatcherFrame();
        mainWindow.Closed += (s, e) => frame.Continue = false;
        System.Windows.Threading.Dispatcher.PushFrame(frame);

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
