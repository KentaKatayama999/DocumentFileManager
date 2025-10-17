using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentFileManager.UI.Services
{
    /// <summary>
    /// データ整合性サービスの実装
    /// データベースと物理ファイルシステムの整合性を検証・修復する
    /// </summary>
    public class DataIntegrityService : IDataIntegrityService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ICheckItemDocumentRepository _checkItemDocumentRepository;
        private readonly PathSettings _pathSettings;
        private readonly ILogger<DataIntegrityService> _logger;
        private readonly string _projectRoot;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DataIntegrityService(
            IDocumentRepository documentRepository,
            ICheckItemDocumentRepository checkItemDocumentRepository,
            IOptions<PathSettings> pathSettings,
            ILogger<DataIntegrityService> logger)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _checkItemDocumentRepository = checkItemDocumentRepository ?? throw new ArgumentNullException(nameof(checkItemDocumentRepository));
            _pathSettings = pathSettings?.Value ?? throw new ArgumentNullException(nameof(pathSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // プロジェクトルートパスを計算
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var levelsUp = _pathSettings.ProjectRootLevelsUp;
            _projectRoot = currentDir;
            for (int i = 0; i < levelsUp; i++)
            {
                var parent = Directory.GetParent(_projectRoot);
                if (parent != null)
                {
                    _projectRoot = parent.FullName;
                }
            }
        }

        /// <summary>
        /// データ整合性をチェックする
        /// </summary>
        public async Task<IntegrityReport> CheckIntegrityAsync()
        {
            _logger.LogInformation("データ整合性チェックを開始します");

            var report = new IntegrityReport
            {
                CheckedAt = DateTime.Now
            };

            try
            {
                // 物理ファイルが見つからない文書を検索
                report.MissingFiles = await FindMissingFilesAsync();

                // 孤立したキャプチャ画像を検索
                report.OrphanedCaptures = await FindOrphanedCapturesAsync();

                _logger.LogInformation(
                    "データ整合性チェック完了: 見つからないファイル={MissingCount}件, 孤立キャプチャ={OrphanedCount}件",
                    report.MissingFiles.Count,
                    report.OrphanedCaptures.Count);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "データ整合性チェック中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// 物理ファイルが見つからない文書を検索する
        /// </summary>
        public async Task<List<Document>> FindMissingFilesAsync()
        {
            _logger.LogDebug("物理ファイルが見つからない文書を検索中...");

            var allDocuments = await _documentRepository.GetAllAsync();
            var missingFiles = new List<Document>();

            foreach (var doc in allDocuments)
            {
                var absolutePath = doc.GetAbsolutePath(_projectRoot);
                if (!File.Exists(absolutePath))
                {
                    _logger.LogWarning("文書ファイルが見つかりません: {FilePath} (ID={Id})", absolutePath, doc.Id);
                    missingFiles.Add(doc);
                }
            }

            _logger.LogDebug("見つからない文書ファイル: {Count}件", missingFiles.Count);
            return missingFiles;
        }

        /// <summary>
        /// 孤立したキャプチャ画像を検索する
        /// </summary>
        public async Task<List<string>> FindOrphanedCapturesAsync()
        {
            _logger.LogDebug("孤立したキャプチャ画像を検索中...");

            var orphanedCaptures = new List<string>();
            var capturesDirectory = Path.Combine(_projectRoot, _pathSettings.CapturesDirectory);

            // capturesディレクトリが存在しない場合は空リストを返す
            if (!Directory.Exists(capturesDirectory))
            {
                _logger.LogDebug("キャプチャディレクトリが存在しません: {Path}", capturesDirectory);
                return orphanedCaptures;
            }

            // すべてのキャプチャファイルを取得
            var captureFiles = Directory.GetFiles(capturesDirectory, "*.png", SearchOption.AllDirectories);
            _logger.LogDebug("キャプチャファイル総数: {Count}件", captureFiles.Length);

            // DBに登録されているキャプチャファイルのセットを作成
            var allDocuments = await _documentRepository.GetAllAsync();
            var registeredCaptures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doc in allDocuments)
            {
                var checkItems = await _checkItemDocumentRepository.GetByDocumentIdAsync(doc.Id);
                foreach (var item in checkItems)
                {
                    if (!string.IsNullOrEmpty(item.CaptureFile))
                    {
                        var absolutePath = item.GetCaptureAbsolutePath(_projectRoot);
                        if (absolutePath != null)
                        {
                            registeredCaptures.Add(absolutePath);
                        }
                    }
                }
            }

            // 物理ファイルがDBに存在しない場合は孤立している
            foreach (var captureFile in captureFiles)
            {
                if (!registeredCaptures.Contains(captureFile))
                {
                    _logger.LogWarning("孤立したキャプチャファイル: {FilePath}", captureFile);
                    orphanedCaptures.Add(captureFile);
                }
            }

            _logger.LogDebug("孤立したキャプチャ画像: {Count}件", orphanedCaptures.Count);
            return orphanedCaptures;
        }

        /// <summary>
        /// データ整合性を修復する
        /// </summary>
        public async Task RepairIntegrityAsync(IntegrityReport report, RepairOptions options)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger.LogInformation("データ整合性修復を開始します");

            try
            {
                // 見つからない文書ファイルのDBレコードを削除
                if (options.RemoveMissingDocuments && report.MissingFiles.Count > 0)
                {
                    _logger.LogInformation("見つからない文書ファイルのレコードを削除します: {Count}件", report.MissingFiles.Count);

                    foreach (var doc in report.MissingFiles)
                    {
                        // 関連するCheckItemDocumentも削除される（カスケード削除）
                        await _documentRepository.DeleteAsync(doc.Id);
                        _logger.LogDebug("文書を削除しました: ID={Id}, RelativePath={RelativePath}", doc.Id, doc.RelativePath);
                    }

                    await _documentRepository.SaveChangesAsync();
                    _logger.LogInformation("見つからない文書ファイルのレコード削除完了");
                }

                // 孤立したキャプチャ画像を削除
                if (options.RemoveOrphanedCaptures && report.OrphanedCaptures.Count > 0)
                {
                    _logger.LogInformation("孤立したキャプチャ画像を削除します: {Count}件", report.OrphanedCaptures.Count);

                    foreach (var captureFile in report.OrphanedCaptures)
                    {
                        try
                        {
                            if (File.Exists(captureFile))
                            {
                                File.Delete(captureFile);
                                _logger.LogDebug("孤立キャプチャを削除しました: {FilePath}", captureFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "孤立キャプチャの削除に失敗しました: {FilePath}", captureFile);
                        }
                    }

                    _logger.LogInformation("孤立したキャプチャ画像の削除完了");
                }

                _logger.LogInformation("データ整合性修復が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "データ整合性修復中にエラーが発生しました");
                throw;
            }
        }
    }
}
