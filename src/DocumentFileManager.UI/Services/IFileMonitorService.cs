using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentFileManager.UI.Services
{
    /// <summary>
    /// ファイル監視サービスのインターフェース
    /// ファイルシステムの変更を監視し、イベントを通知する
    /// </summary>
    public interface IFileMonitorService
    {
        /// <summary>
        /// ファイル監視を開始する
        /// </summary>
        /// <param name="directoryPath">監視対象ディレクトリのパス</param>
        Task StartMonitoringAsync(string directoryPath);

        /// <summary>
        /// ファイル監視を停止する
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// ファイルが変更されたときに発生するイベント
        /// </summary>
        event EventHandler<FileSystemEventArgs>? FileChanged;

        /// <summary>
        /// ファイルが削除されたときに発生するイベント
        /// </summary>
        event EventHandler<FileSystemEventArgs>? FileDeleted;

        /// <summary>
        /// ファイルがリネームされたときに発生するイベント
        /// </summary>
        event EventHandler<RenamedEventArgs>? FileRenamed;

        /// <summary>
        /// 監視中かどうかを示す
        /// </summary>
        bool IsMonitoring { get; }
    }
}
