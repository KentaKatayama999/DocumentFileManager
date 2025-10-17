using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace DocumentFileManager.UI.Services
{
    /// <summary>
    /// ファイルシステム監視サービスの実装
    /// FileSystemWatcherを使用してファイル変更を検出する
    /// </summary>
    public class FileMonitorService : IFileMonitorService, IDisposable
    {
        private readonly ILogger _logger;
        private FileSystemWatcher? _watcher;
        private bool _isMonitoring;
        private string? _monitoredPath;

        /// <summary>
        /// ファイルが変更されたときに発生するイベント
        /// </summary>
        public event EventHandler<FileSystemEventArgs>? FileChanged;

        /// <summary>
        /// ファイルが削除されたときに発生するイベント
        /// </summary>
        public event EventHandler<FileSystemEventArgs>? FileDeleted;

        /// <summary>
        /// ファイルがリネームされたときに発生するイベント
        /// </summary>
        public event EventHandler<RenamedEventArgs>? FileRenamed;

        /// <summary>
        /// 監視中かどうかを示す
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        public FileMonitorService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// ファイル監視を開始する
        /// </summary>
        /// <param name="directoryPath">監視対象ディレクトリのパス</param>
        public Task StartMonitoringAsync(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                _logger.Warning("監視対象ディレクトリが存在しません: {Path}", directoryPath);
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            if (_isMonitoring)
            {
                _logger.Warning("既にファイル監視が開始されています: {Path}", _monitoredPath);
                return Task.CompletedTask;
            }

            try
            {
                _watcher = new FileSystemWatcher(directoryPath)
                {
                    NotifyFilter = NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = false
                };

                // イベントハンドラの登録
                _watcher.Changed += OnFileChanged;
                _watcher.Deleted += OnFileDeleted;
                _watcher.Renamed += OnFileRenamed;
                _watcher.Error += OnError;

                // 監視開始
                _watcher.EnableRaisingEvents = true;
                _isMonitoring = true;
                _monitoredPath = directoryPath;

                _logger.Information("ファイル監視を開始しました: {Path}", directoryPath);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ファイル監視の開始に失敗しました: {Path}", directoryPath);
                throw;
            }
        }

        /// <summary>
        /// ファイル監視を停止する
        /// </summary>
        public Task StopMonitoringAsync()
        {
            if (!_isMonitoring || _watcher == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFileChanged;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Error -= OnError;

                _watcher.Dispose();
                _watcher = null;
                _isMonitoring = false;

                _logger.Information("ファイル監視を停止しました: {Path}", _monitoredPath);
                _monitoredPath = null;

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ファイル監視の停止に失敗しました");
                throw;
            }
        }

        /// <summary>
        /// ファイル変更イベントハンドラ
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.Debug("ファイル変更を検出: {FullPath} ({ChangeType})", e.FullPath, e.ChangeType);
            FileChanged?.Invoke(this, e);
        }

        /// <summary>
        /// ファイル削除イベントハンドラ
        /// </summary>
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            _logger.Information("ファイル削除を検出: {FullPath}", e.FullPath);
            FileDeleted?.Invoke(this, e);
        }

        /// <summary>
        /// ファイルリネームイベントハンドラ
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            _logger.Information("ファイルリネームを検出: {OldFullPath} -> {FullPath}", e.OldFullPath, e.FullPath);
            FileRenamed?.Invoke(this, e);
        }

        /// <summary>
        /// エラーイベントハンドラ
        /// </summary>
        private void OnError(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            _logger.Error(exception, "FileSystemWatcherでエラーが発生しました");
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            if (_watcher != null)
            {
                StopMonitoringAsync().GetAwaiter().GetResult();
            }
        }
    }
}
