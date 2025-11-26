using System.IO;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// チェックリスト管理サービスの実装
/// </summary>
public class ChecklistService : IChecklistService
{
    private readonly ICheckItemRepository _checkItemRepository;
    private readonly Infrastructure.Services.ChecklistSaver _checklistSaver;
    private readonly PathSettings _pathSettings;
    private readonly ILogger<ChecklistService> _logger;
    private readonly string _documentRootPath;

    public ChecklistService(
        ICheckItemRepository checkItemRepository,
        Infrastructure.Services.ChecklistSaver checklistSaver,
        PathSettings pathSettings,
        ILogger<ChecklistService> logger,
        string documentRootPath)
    {
        _checkItemRepository = checkItemRepository;
        _checklistSaver = checklistSaver;
        _pathSettings = pathSettings;
        _logger = logger;
        _documentRootPath = documentRootPath;
    }

    /// <summary>
    /// 新規チェックリストを作成
    /// </summary>
    public async Task<ChecklistCreationResult> CreateNewChecklistAsync(string checklistName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(checklistName))
            {
                return new ChecklistCreationResult
                {
                    Success = false,
                    ErrorMessage = "チェックリスト名を入力してください"
                };
            }

            _logger.LogInformation("新規チェックリストを作成: {ChecklistName}", checklistName);

            // ファイル名を生成（checklist_xxx.json形式）
            // パストラバーサル対策: 不正な文字を除去
            var safeFileName = string.Concat(checklistName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"checklist_{safeFileName}.json";

            var filePath = Path.Combine(_documentRootPath, fileName);
            var absolutePath = Path.GetFullPath(filePath);

            // パストラバーサル対策: ファイルパスがdocumentRootPath配下であることを確認
            if (!absolutePath.StartsWith(Path.GetFullPath(_documentRootPath), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("不正なパスが検出されました: {FilePath}", absolutePath);
                return new ChecklistCreationResult
                {
                    Success = false,
                    ErrorMessage = "不正なチェックリスト名です"
                };
            }

            bool overwritten = File.Exists(absolutePath);

            // 空のチェックリストを作成
            var emptyCheckItems = new List<CheckItem>();

            // JSON形式で保存
            await _checklistSaver.SaveAsync(emptyCheckItems, absolutePath);

            _logger.LogInformation("新規チェックリストファイルを作成しました: {FilePath}", absolutePath);

            // 設定を更新して新しいチェックリストを使用
            _pathSettings.SelectedChecklistFile = fileName;

            // データベースの既存チェック項目をクリア（新しいチェックリスト用）
            var existingItems = await _checkItemRepository.GetAllWithChildrenAsync();
            foreach (var item in existingItems)
            {
                await _checkItemRepository.DeleteAsync(item.Id);
            }

            _logger.LogInformation("既存のチェック項目をクリアしました");

            return new ChecklistCreationResult
            {
                Success = true,
                FileName = fileName,
                FilePath = absolutePath,
                Overwritten = overwritten
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新規チェックリストの作成に失敗しました");
            return new ChecklistCreationResult
            {
                Success = false,
                ErrorMessage = $"新規チェックリストの作成に失敗しました: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 指定したファイル名のチェックリストが存在するかどうかを確認
    /// </summary>
    public bool ChecklistExists(string fileName)
    {
        var filePath = Path.Combine(_documentRootPath, fileName);
        return File.Exists(filePath);
    }
}
