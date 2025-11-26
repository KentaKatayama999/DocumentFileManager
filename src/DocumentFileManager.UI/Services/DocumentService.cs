using System.IO;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// 資料登録サービスの実装
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentService> _logger;
    private readonly string _documentRootPath;

    public DocumentService(
        IDocumentRepository documentRepository,
        ILogger<DocumentService> logger,
        string documentRootPath)
    {
        _documentRepository = documentRepository;
        _logger = logger;
        _documentRootPath = documentRootPath;
    }

    /// <summary>
    /// 資料を登録
    /// </summary>
    public async Task<DocumentRegistrationResult> RegisterDocumentAsync(string filePath)
    {
        try
        {
            // ファイル存在チェック
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("ファイルが見つかりません: {FilePath}", filePath);
                return new DocumentRegistrationResult
                {
                    Success = false,
                    ErrorMessage = $"ファイルが見つかりません: {filePath}"
                };
            }

            // ファイル名と拡張子を取得
            var fileName = Path.GetFileName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            // コピー先のパスを決定（重複があれば連番を追加）
            var destFileName = fileName;
            var destPath = Path.Combine(_documentRootPath, destFileName);
            var counter = 1;

            while (File.Exists(destPath))
            {
                destFileName = $"{fileNameWithoutExt}_{counter}{extension}";
                destPath = Path.Combine(_documentRootPath, destFileName);
                counter++;
            }

            // ファイル名のみを相対パスとして保存
            var relativePath = destFileName;

            // 重複チェック（ファイル名で）
            var existing = await _documentRepository.GetByRelativePathAsync(relativePath);
            if (existing != null)
            {
                _logger.LogInformation("既に登録済みの資料です: {RelativePath}", relativePath);
                return new DocumentRegistrationResult
                {
                    Success = false,
                    Skipped = true,
                    ErrorMessage = $"この資料は既に登録されています: {relativePath}"
                };
            }

            // documentRootPathにファイルをコピー（元のファイルと異なる場合のみ）
            var sourceFullPath = Path.GetFullPath(filePath);
            var destFullPath = Path.GetFullPath(destPath);

            // パストラバーサル対策: コピー先がdocumentRootPath配下であることを確認
            if (!destFullPath.StartsWith(Path.GetFullPath(_documentRootPath), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("不正なパスが検出されました: {DestPath}", destFullPath);
                return new DocumentRegistrationResult
                {
                    Success = false,
                    ErrorMessage = "不正なファイルパスです"
                };
            }

            if (!string.Equals(sourceFullPath, destFullPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(filePath, destPath, overwrite: false);
                _logger.LogInformation("ファイルをコピーしました: {Source} -> {Dest}", filePath, destPath);
            }
            else
            {
                _logger.LogInformation("ファイルは既にdocumentRoot内にあります: {Path}", filePath);
            }

            // Document エンティティ作成
            var document = new Document
            {
                FileName = destFileName,
                RelativePath = relativePath,
                FileType = extension,
                AddedAt = DateTime.UtcNow
            };

            // データベースに追加
            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            _logger.LogInformation("資料を登録しました: {FileName} ({RelativePath})", document.FileName, document.RelativePath);

            return new DocumentRegistrationResult
            {
                Success = true,
                Document = document
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料の登録に失敗しました: {FilePath}", filePath);
            return new DocumentRegistrationResult
            {
                Success = false,
                ErrorMessage = $"資料の登録に失敗しました: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 複数の資料を一括登録
    /// </summary>
    public async Task<List<DocumentRegistrationResult>> RegisterDocumentsAsync(IEnumerable<string> filePaths)
    {
        var results = new List<DocumentRegistrationResult>();
        foreach (var filePath in filePaths)
        {
            var result = await RegisterDocumentAsync(filePath);
            results.Add(result);
        }
        return results;
    }
}
