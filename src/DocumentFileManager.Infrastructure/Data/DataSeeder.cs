using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Models;
using DocumentFileManager.Infrastructure.Services;
using DocumentFileManager.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.Infrastructure.Data;

/// <summary>
/// 開発・デモ用のシードデータを投入するクラス
/// </summary>
public class DataSeeder
{
    private readonly DocumentManagerContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _projectRoot;
    private readonly string _checklistFile;

    public DataSeeder(DocumentManagerContext context, ILoggerFactory loggerFactory, string projectRoot, string checklistFile = "checklist.json")
    {
        _context = context;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DataSeeder>();
        _projectRoot = projectRoot;
        _checklistFile = checklistFile;
    }

    /// <summary>
    /// シードデータを投入する
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("シードデータの投入を開始します");

        // チェック項目は常にJSONと同期
        await SyncCheckItemsAsync();

        // 資料とその紐づけは初回のみ投入
        if (!await _context.Documents.AnyAsync())
        {
            await SeedDocumentsAsync();
            await SeedCheckItemDocumentsAsync();
        }
        else
        {
            _logger.LogInformation("資料データは既に存在するため、投入をスキップします");
        }

        _logger.LogInformation("シードデータの投入が完了しました");
    }

    /// <summary>
    /// 資料データを投入する（dummyフォルダから）
    /// </summary>
    private async Task SeedDocumentsAsync()
    {
        _logger.LogInformation("資料データを投入します");

        var dummyFolder = Path.Combine(_projectRoot, "dummy");
        if (!Directory.Exists(dummyFolder))
        {
            _logger.LogWarning("dummyフォルダが見つかりません: {Path}", dummyFolder);
            return;
        }

        var files = new[]
        {
            "設計書_rev1.pdf",
            "仕様書_最新版.pdf",
            "テスト計画書.docx"
        };

        foreach (var fileName in files)
        {
            var filePath = Path.Combine(dummyFolder, fileName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("ファイルが見つかりません: {FileName}", fileName);
                continue;
            }

            var relativePath = Path.Combine("dummy", fileName);
            var document = new Document
            {
                FileName = fileName,
                RelativePath = relativePath,
                FileType = Path.GetExtension(fileName),
                AddedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 30))
            };

            await _context.Documents.AddAsync(document);
            _logger.LogDebug("資料を追加: {FileName}", fileName);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("資料データの投入が完了しました");
    }

    /// <summary>
    /// チェック項目データをJSONファイルと同期する
    /// </summary>
    private async Task SyncCheckItemsAsync()
    {
        _logger.LogInformation("チェック項目データをJSONと同期します");

        var checklistPath = Path.Combine(_projectRoot, _checklistFile);
        if (!File.Exists(checklistPath))
        {
            _logger.LogWarning("{ChecklistFile} が見つかりません: {Path}", _checklistFile, checklistPath);
            return;
        }

        // ChecklistLoaderでJSONを読み込み
        var checklistLogger = _loggerFactory.CreateLogger<ChecklistLoader>();
        var loader = new ChecklistLoader(checklistLogger);
        var definitions = await loader.LoadAsync(checklistPath);

        var stats = loader.GetStatistics(definitions);
        _logger.LogInformation("JSON統計: 分類={Categories}件, 項目={Items}件, チェック済={Checked}件",
            stats.TotalCategories, stats.TotalItems, stats.CheckedItems);

        // JSONに含まれる全パスを収集
        var jsonPaths = new HashSet<string>();

        // 階層構造を再帰的に同期
        await SyncCheckItemsRecursiveAsync(definitions, null, null, jsonPaths);

        // JSONにないDB項目を削除（全件取得せずにSQLで直接削除）
        var itemsToDelete = await _context.CheckItems
            .Where(item => !jsonPaths.Contains(item.Path))
            .ToListAsync();

        if (itemsToDelete.Any())
        {
            _logger.LogInformation("JSONに存在しない {Count} 件のチェック項目を削除します", itemsToDelete.Count);
            _context.CheckItems.RemoveRange(itemsToDelete);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("チェック項目データの同期が完了しました");
    }

    /// <summary>
    /// チェック項目を再帰的にDBと同期する（バッチ処理版・N+1問題解決）
    /// </summary>
    /// <param name="definitions">チェック項目定義リスト</param>
    /// <param name="parentPath">親のパス</param>
    /// <param name="parentId">親のID</param>
    /// <param name="jsonPaths">JSONに含まれるパスのセット</param>
    private async Task SyncCheckItemsRecursiveAsync(
        List<CheckItemDefinition> definitions,
        string? parentPath,
        int? parentId,
        HashSet<string> jsonPaths)
    {
        // 同じ階層のすべてのパスを先に生成
        var pathsToSync = definitions.Select(def =>
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? def.Label : $"{parentPath}/{def.Label}";
            return (def, currentPath);
        }).ToList();

        // この階層のすべてのパスをJSONパスセットに追加
        foreach (var (_, currentPath) in pathsToSync)
        {
            jsonPaths.Add(currentPath);
        }

        // この階層のすべての既存項目を一度に取得（N+1問題解決）
        var paths = pathsToSync.Select(x => x.currentPath).ToList();
        var existingItems = await _context.CheckItems
            .Where(c => paths.Contains(c.Path))
            .ToDictionaryAsync(c => c.Path);

        // この階層のすべての項目を処理（メモリ上で）
        var checkItems = new List<CheckItem>();
        foreach (var (def, currentPath) in pathsToSync)
        {
            CheckItem checkItem;

            if (existingItems.TryGetValue(currentPath, out var existingItem))
            {
                // 既存項目を更新（Statusは維持）
                existingItem.Label = def.Label;
                existingItem.ParentId = parentId;
                checkItem = existingItem;

                _logger.LogDebug("チェック項目を更新: {Path} (ID={Id}, Status={Status}は維持)",
                    checkItem.Path, checkItem.Id, checkItem.Status);
            }
            else
            {
                // 新規項目を追加（JSONのCheckedフラグに従う）
                checkItem = new CheckItem
                {
                    Path = currentPath,
                    Label = def.Label,
                    Status = def.Checked ? ItemStatus.Current : ItemStatus.Unspecified,
                    ParentId = parentId
                };

                await _context.CheckItems.AddAsync(checkItem);

                _logger.LogDebug("チェック項目を追加: {Path} (Status={Status})",
                    checkItem.Path, checkItem.Status);
            }

            checkItems.Add(checkItem);
        }

        // この階層のすべての変更を一度に保存（バッチ処理）
        await _context.SaveChangesAsync();
        _logger.LogDebug("階層 {ParentPath} の {Count} 項目を保存しました", parentPath ?? "ルート", checkItems.Count);

        // 子要素を再帰的に処理
        for (int i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var checkItem = checkItems[i];

            if (def.Children != null && def.Children.Count > 0)
            {
                await SyncCheckItemsRecursiveAsync(def.Children, checkItem.Path, checkItem.Id, jsonPaths);
            }
        }
    }

    /// <summary>
    /// チェック項目と資料の紐づけデータを投入する
    /// </summary>
    private async Task SeedCheckItemDocumentsAsync()
    {
        _logger.LogInformation("紐づけデータを投入します");

        // 設計書 と 設計図面/建築図面/平面図/1階平面図 を紐づけ
        var designDoc = await _context.Documents.FirstOrDefaultAsync(d => d.FileName == "設計書_rev1.pdf");
        var floorPlan = await _context.CheckItems.FirstOrDefaultAsync(c => c.Path == "設計図面/建築図面/平面図/1階平面図");

        if (designDoc != null && floorPlan != null)
        {
            var link1 = new CheckItemDocument
            {
                CheckItemId = floorPlan.Id,
                DocumentId = designDoc.Id,
                LinkedAt = DateTime.UtcNow.AddDays(-5),
                CaptureFile = "dummy/picture/capture_001.png"
            };
            await _context.CheckItemDocuments.AddAsync(link1);
            _logger.LogDebug("紐づけを追加: {DocName} ⇔ {CheckItemPath}", designDoc.FileName, floorPlan.Path);
        }

        // 仕様書 と 仕様書/設備仕様/電気設備仕様/照明器具 を紐づけ
        var specDoc = await _context.Documents.FirstOrDefaultAsync(d => d.FileName == "仕様書_最新版.pdf");
        var lighting = await _context.CheckItems.FirstOrDefaultAsync(c => c.Path == "仕様書/設備仕様/電気設備仕様/照明器具");

        if (specDoc != null && lighting != null)
        {
            var link2 = new CheckItemDocument
            {
                CheckItemId = lighting.Id,
                DocumentId = specDoc.Id,
                LinkedAt = DateTime.UtcNow.AddDays(-3),
                CaptureFile = "dummy/picture/capture_002.png"
            };
            await _context.CheckItemDocuments.AddAsync(link2);
            _logger.LogDebug("紐づけを追加: {DocName} ⇔ {CheckItemPath}", specDoc.FileName, lighting.Path);
        }

        // テスト計画書 と テスト/単体テスト/ドメイン層/CheckItemテスト を紐づけ
        var testDoc = await _context.Documents.FirstOrDefaultAsync(d => d.FileName == "テスト計画書.docx");
        var checkItemTest = await _context.CheckItems.FirstOrDefaultAsync(c => c.Path == "テスト/単体テスト/ドメイン層/CheckItemテスト");

        if (testDoc != null && checkItemTest != null)
        {
            var link3 = new CheckItemDocument
            {
                CheckItemId = checkItemTest.Id,
                DocumentId = testDoc.Id,
                LinkedAt = DateTime.UtcNow.AddDays(-1)
                // CaptureFileなし
            };
            await _context.CheckItemDocuments.AddAsync(link3);
            _logger.LogDebug("紐づけを追加: {DocName} ⇔ {CheckItemPath}", testDoc.FileName, checkItemTest.Path);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("紐づけデータの投入が完了しました");
    }
}
