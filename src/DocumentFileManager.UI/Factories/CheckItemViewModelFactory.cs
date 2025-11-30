using System.IO;
using DocumentFileManager.Entities;
using DocumentFileManager.UI.Models;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Factories;

/// <summary>
/// CheckItemViewModelを生成するファクトリ
/// Entity→ViewModel変換とCheckItemStateの初期化を担当
/// </summary>
public class CheckItemViewModelFactory : ICheckItemViewModelFactory
{
    private readonly string _documentRootPath;
    private readonly ILogger<CheckItemViewModelFactory>? _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="documentRootPath">ドキュメントルートパス</param>
    /// <param name="logger">ロガー（オプション）</param>
    public CheckItemViewModelFactory(string documentRootPath, ILogger<CheckItemViewModelFactory>? logger = null)
    {
        _documentRootPath = documentRootPath ?? throw new ArgumentNullException(nameof(documentRootPath));
        _logger = logger;
    }

    /// <inheritdoc />
    public CheckItemViewModel Create(
        CheckItem entity,
        WindowMode windowMode,
        CheckItemDocument? checkItemDocument = null)
    {
        var isMainWindow = windowMode == WindowMode.MainWindow;
        var viewModel = new CheckItemViewModel(entity, _documentRootPath, isMainWindow);

        // 紐づけデータから状態を設定
        if (checkItemDocument != null)
        {
            if (!isMainWindow)
            {
                viewModel.IsChecked = checkItemDocument.IsChecked;
            }
            viewModel.CaptureFilePath = checkItemDocument.CaptureFile;
        }

        // CaptureFileExistsを更新（ファイル存在チェックは初期化時に1回のみ）
        UpdateCaptureFileExists(viewModel);

        // ItemStateを適切に設定
        UpdateItemStateFromCheckState(viewModel, checkItemDocument);

        _logger?.LogDebug("ViewModel created: Id={Id}, WindowMode={WindowMode}, ItemState={ItemState}",
            entity.Id, windowMode, viewModel.State.ItemState);

        return viewModel;
    }

    /// <inheritdoc />
    public List<CheckItemViewModel> CreateHierarchy(
        List<CheckItem> entities,
        WindowMode windowMode,
        Dictionary<int, CheckItemDocument>? checkItemDocuments = null)
    {
        var viewModels = new List<CheckItemViewModel>();

        foreach (var entity in entities)
        {
            CheckItemDocument? checkItemDocument = null;
            checkItemDocuments?.TryGetValue(entity.Id, out checkItemDocument);

            var viewModel = Create(entity, windowMode, checkItemDocument);

            // 子要素を再帰的に追加
            if (entity.Children != null && entity.Children.Count > 0)
            {
                var childViewModels = CreateHierarchy(entity.Children.ToList(), windowMode, checkItemDocuments);
                foreach (var child in childViewModels)
                {
                    viewModel.Children.Add(child);
                }
            }

            viewModels.Add(viewModel);
        }

        _logger?.LogInformation("Hierarchy created: {Count} root items", viewModels.Count);

        return viewModels;
    }

    /// <summary>
    /// キャプチャファイルの存在をチェックしてStateを更新
    /// </summary>
    private void UpdateCaptureFileExists(CheckItemViewModel viewModel)
    {
        if (!string.IsNullOrEmpty(viewModel.CaptureFilePath))
        {
            var absolutePath = viewModel.GetCaptureAbsolutePath();
            var exists = !string.IsNullOrEmpty(absolutePath) && File.Exists(absolutePath);
            viewModel.UpdateCaptureFileExists(exists);
        }
    }

    /// <summary>
    /// チェック状態とキャプチャ状態からItemStateを決定
    /// </summary>
    private void UpdateItemStateFromCheckState(CheckItemViewModel viewModel, CheckItemDocument? checkItemDocument)
    {
        var isChecked = viewModel.IsChecked;
        var hasCaptureFile = viewModel.State.CaptureFileExists;

        // ItemState状態コード決定
        // 00: 未紐づけ
        // 10: チェックON、キャプチャなし
        // 11: チェックON、キャプチャあり
        // 20: チェックOFF（履歴あり）、キャプチャなし
        // 22: チェックOFF（履歴あり）、キャプチャあり

        string itemState;
        if (checkItemDocument == null)
        {
            // 紐づけなし
            itemState = "00";
        }
        else if (isChecked)
        {
            // チェックON
            itemState = hasCaptureFile ? "11" : "10";
        }
        else
        {
            // チェックOFF（履歴あり）
            itemState = hasCaptureFile ? "22" : "20";
        }

        viewModel.UpdateItemState(itemState);
    }
}
