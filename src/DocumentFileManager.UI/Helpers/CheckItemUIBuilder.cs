using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Factories;
using DocumentFileManager.UI.Models;
using DocumentFileManager.UI.Services.Abstractions;
using DocumentFileManager.UI.ViewModels;
using DocumentFileManager.UI.Windows;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Helpers;

/// <summary>
/// チェック項目のUI階層を動的に構築するヘルパークラス
/// MVVMパターンに準拠：UI構築とバインディング設定のみを担当
/// </summary>
public class CheckItemUIBuilder
{
    private readonly ICheckItemRepository _repository;
    private readonly ICheckItemDocumentRepository _checkItemDocumentRepository;
    private readonly IChecklistStateManager _stateManager;
    private readonly ICheckItemViewModelFactory _viewModelFactory;
    private readonly UISettings _settings;
    private readonly ILogger<CheckItemUIBuilder> _logger;
    private readonly string _documentRootPath;
    private Document? _currentDocument;
    private FrameworkElement? _containerElement;  // DataTemplateを取得するためのリソース元

    /// <summary>
    /// キャプチャ要求時のコールバック
    /// </summary>
    public Func<CheckItemViewModel, Task>? OnCaptureRequested { get; set; }

    /// <summary>
    /// 選択時のコールバック（MainWindow用）
    /// </summary>
    public Func<CheckItemViewModel, Task>? OnItemSelected { get; set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public CheckItemUIBuilder(
        ICheckItemRepository repository,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        IChecklistStateManager stateManager,
        ICheckItemViewModelFactory viewModelFactory,
        UISettings settings,
        ILogger<CheckItemUIBuilder> logger,
        string documentRootPath)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _checkItemDocumentRepository = checkItemDocumentRepository ?? throw new ArgumentNullException(nameof(checkItemDocumentRepository));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentRootPath = documentRootPath ?? throw new ArgumentNullException(nameof(documentRootPath));
    }

    /// <summary>
    /// チェック項目の階層UIを構築する
    /// </summary>
    /// <param name="containerPanel">親となるPanel</param>
    /// <param name="document">紐づけるDocumentオブジェクト（nullの場合はMainWindow）</param>
    public async Task BuildAsync(Panel containerPanel, Document? document = null)
    {
        _currentDocument = document;
        _containerElement = containerPanel;  // DataTemplateのリソース元として保存

        if (document != null)
        {
            _logger.LogInformation("チェック項目UIの構築を開始します (Document: {DocumentId})", document.Id);
        }
        else
        {
            _logger.LogInformation("チェック項目UIの構築を開始します（全体表示）");
        }

        containerPanel.Children.Clear();

        // ルート項目を取得
        var rootItems = await _repository.GetRootItemsAsync();
        _logger.LogInformation("{Count} 件のルート項目を取得しました", rootItems.Count);

        // Documentと紐づいたチェック項目を取得
        Dictionary<int, CheckItemDocument>? checkItemDocuments = null;
        if (document != null)
        {
            var linkedItems = await _checkItemDocumentRepository.GetByDocumentIdAsync(document.Id);
            checkItemDocuments = linkedItems.ToDictionary(x => x.CheckItemId);
            _logger.LogInformation("{Count} 件の紐づけデータを取得しました", linkedItems.Count);
        }
        else
        {
            // MainWindow（全体表示）の場合：各チェック項目の最新キャプチャを取得
            var allLinkedItems = await _checkItemDocumentRepository.GetAllAsync();
            checkItemDocuments = allLinkedItems
                .Where(x => x.CaptureFile != null)
                .GroupBy(x => x.CheckItemId)
                .Select(g => g.OrderByDescending(x => x.LinkedAt).First())
                .ToDictionary(x => x.CheckItemId);
            _logger.LogInformation("全体表示モード：{Count} 件のチェック項目に最新キャプチャがあります", checkItemDocuments.Count);
        }

        // ViewModelに変換（Factoryを使用）
        var windowMode = document == null ? WindowMode.MainWindow : WindowMode.ChecklistWindow;
        var viewModels = _viewModelFactory.CreateHierarchy(rootItems, windowMode, checkItemDocuments);

        // コマンドを設定
        SetupCommandsForHierarchy(viewModels);

        // UIを構築
        foreach (var viewModel in viewModels)
        {
            var groupBox = CreateGroupBox(viewModel, 0);

            if (groupBox is GroupBox rootGroupBox)
            {
                rootGroupBox.MinWidth = _settings.GroupBox.RootMinWidth;
            }

            containerPanel.Children.Add(groupBox);
        }

        _logger.LogInformation("チェック項目UIの構築が完了しました");
    }

    /// <summary>
    /// ViewModel階層全体にコマンドを設定する
    /// </summary>
    private void SetupCommandsForHierarchy(List<CheckItemViewModel> viewModels)
    {
        foreach (var viewModel in viewModels)
        {
            if (viewModel.IsItem)
            {
                SetupCommands(viewModel);
            }

            if (viewModel.Children.Count > 0)
            {
                SetupCommandsForHierarchy(viewModel.Children.ToList());
            }
        }
    }

    /// <summary>
    /// ViewModelにコマンドを設定する
    /// </summary>
    private void SetupCommands(CheckItemViewModel viewModel)
    {
        if (viewModel.IsMainWindow)
        {
            // MainWindow用: SelectCommand（資料フィルタリング）
            viewModel.SelectCommand = new AsyncRelayCommand(async () =>
            {
                _logger.LogDebug("SelectCommand実行: CheckItemId={CheckItemId}", viewModel.Id);
                if (OnItemSelected != null)
                {
                    await OnItemSelected(viewModel);
                }
            });
        }
        else
        {
            // ChecklistWindow用: CheckedChangedCommand（チェック状態変更）
            viewModel.CheckedChangedCommand = new AsyncRelayCommand(async () =>
            {
                if (_currentDocument == null)
                {
                    _logger.LogWarning("Documentがnullのためチェック状態変更をスキップします");
                    return;
                }

                _logger.LogDebug("CheckedChangedCommand実行: CheckItemId={CheckItemId}, IsChecked={IsChecked}",
                    viewModel.Id, viewModel.IsChecked);

                try
                {
                    if (viewModel.IsChecked)
                    {
                        await HandleCheckOnAsync(viewModel);
                    }
                    else
                    {
                        await HandleCheckOffAsync(viewModel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "チェック状態変更中にエラー: CheckItemId={CheckItemId}", viewModel.Id);
                    // エラー時はチェック状態を戻す
                    viewModel.IsChecked = !viewModel.IsChecked;
                    throw;
                }
            });
        }

        // ViewCaptureCommand: キャプチャ表示（共通）
        viewModel.ViewCaptureCommand = new RelayCommand(() =>
        {
            var absolutePath = viewModel.GetCaptureAbsolutePath();
            if (string.IsNullOrEmpty(absolutePath))
            {
                _logger.LogWarning("キャプチャファイルパスが未設定です");
                return;
            }

            _logger.LogInformation("キャプチャ画像を表示: {Path}", absolutePath);

            var viewer = new CaptureImageViewerWindow(absolutePath, null);
            viewer.ShowDialog();

            // 削除された場合はViewModelを更新
            if (viewer.IsDeleted)
            {
                // CaptureFilePathのセッター内でCameraButtonVisibility通知が発火するため
                // UpdateCaptureButton()は不要
                viewModel.CaptureFilePath = null;

                // DB更新
                if (_currentDocument != null)
                {
                    Task.Run(async () =>
                    {
                        var linkedItem = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                            _currentDocument.Id, viewModel.Entity.Id);
                        if (linkedItem != null)
                        {
                            await _checkItemDocumentRepository.UpdateCaptureFileAsync(linkedItem.Id, null);
                            await _checkItemDocumentRepository.SaveChangesAsync();
                        }
                    }).Wait();
                }

                _logger.LogInformation("キャプチャ画像が削除されました");
            }
        });
    }

    /// <summary>
    /// チェックON時の処理
    /// </summary>
    private async Task HandleCheckOnAsync(CheckItemViewModel viewModel)
    {
        var transition = await _stateManager.HandleCheckOnAsync(viewModel, _currentDocument!);

        if (transition == null)
        {
            // キャンセルされた場合、チェック状態を戻す
            viewModel.IsChecked = false;
            _logger.LogInformation("チェックON操作がキャンセルされました");
            return;
        }

        // 状態遷移後のViewModelを更新
        // CaptureFilePathのセッターがUpdateCaptureFileExistsFromPath()を呼び出すが、
        // ファイルシステムI/Oの遅延があるため、明示的にItemStateとCaptureFileExistsを先に設定
        viewModel.UpdateItemState(transition.TargetState);
        viewModel.UpdateCaptureFileExists(!string.IsNullOrEmpty(transition.CaptureFile));
        viewModel.CaptureFilePath = transition.CaptureFile;

        // DBにコミット
        await _stateManager.CommitTransitionAsync(transition);

        // キャプチャがない場合、キャプチャ取得を促す
        if (string.IsNullOrEmpty(transition.CaptureFile) && OnCaptureRequested != null)
        {
            var result = MessageBox.Show(
                "この箇所のキャプチャを取得しますか？",
                "キャプチャ確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await OnCaptureRequested(viewModel);
            }
        }
    }

    /// <summary>
    /// チェックOFF時の処理
    /// </summary>
    private async Task HandleCheckOffAsync(CheckItemViewModel viewModel)
    {
        var transition = await _stateManager.HandleCheckOffAsync(viewModel, _currentDocument!);
        await _stateManager.CommitTransitionAsync(transition);

        // ItemStateを更新（DB保存用の状態コード管理）
        viewModel.UpdateItemState(transition.TargetState);
    }

    /// <summary>
    /// GroupBoxまたはCheckBoxを作成する
    /// </summary>
    private UIElement CreateGroupBox(CheckItemViewModel viewModel, int depth)
    {
        if (viewModel.IsItem)
        {
            return CreateCheckBox(viewModel, depth);
        }

        var allChildrenAreItems = viewModel.Children.All(c => c.IsItem);
        var allChildrenAreCategories = viewModel.Children.All(c => c.IsCategory);
        var childCount = viewModel.Children.Count;

        var groupBox = new GroupBox
        {
            Header = viewModel.Label,
            Margin = new Thickness(
                depth * _settings.GroupBox.MarginDepthMultiplier,
                _settings.GroupBox.MarginTop,
                _settings.GroupBox.MarginRight,
                _settings.GroupBox.MarginBottom),
            Padding = new Thickness(_settings.GroupBox.Padding),
            BorderBrush = allChildrenAreItems ? GetBorderBrush(2) : GetBorderBrush(depth),
            BorderThickness = new Thickness(_settings.GroupBox.BorderThickness)
        };

        Panel containerPanel;
        bool isWrapPanel = false;

        if ((allChildrenAreItems && childCount >= _settings.Layout.WrapPanelItemThreshold) ||
            (allChildrenAreCategories && childCount >= _settings.Layout.WrapPanelCategoryThreshold))
        {
            containerPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            isWrapPanel = true;
        }
        else
        {
            containerPanel = new StackPanel { Orientation = Orientation.Vertical };
        }

        foreach (var child in viewModel.Children)
        {
            var childElement = CreateGroupBox(child, depth + 1);

            if (isWrapPanel)
            {
                if (childElement is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is CheckBox cb)
                {
                    cb.MinWidth = _settings.CheckBox.MinWidth;
                    cb.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else if (childElement is GroupBox childGroupBox)
                {
                    childGroupBox.MinWidth = _settings.GroupBox.ChildItemMinWidth;
                }
            }

            containerPanel.Children.Add(childElement);
        }

        if (isWrapPanel && allChildrenAreItems)
        {
            int columnsPerRow = Math.Min(_settings.Layout.MaxColumnsPerRow, (childCount + 1) / 2);
            double calculatedWidth = columnsPerRow * _settings.Layout.WidthPerColumn + _settings.Layout.GroupBoxExtraPadding;
            groupBox.MinWidth = Math.Min(calculatedWidth, _settings.Layout.MaxCalculatedWidth);
        }
        else if (isWrapPanel && allChildrenAreCategories)
        {
            groupBox.MinWidth = _settings.GroupBox.ChildCategoryMinWidth;
        }

        groupBox.Content = containerPanel;
        return groupBox;
    }

    /// <summary>
    /// チェック項目用のContentControlを作成する
    /// DataTemplateを使用してUIを描画（XAMLで定義されたCheckItemTemplateを使用）
    /// </summary>
    private UIElement CreateCheckBox(CheckItemViewModel viewModel, int depth)
    {
        // ContentControlを作成し、ViewModelをContentとして設定
        var contentControl = new ContentControl
        {
            Content = viewModel,
            Margin = new Thickness(
                depth * _settings.CheckBox.MarginDepthMultiplier + _settings.CheckBox.MarginLeft,
                _settings.CheckBox.MarginTop,
                _settings.CheckBox.MarginRight,
                _settings.CheckBox.MarginBottom),
            Tag = viewModel  // フィルタリング・ハイライト用
        };

        // DataTemplateをWindowリソースから明示的に取得して適用
        if (_containerElement?.TryFindResource("CheckItemTemplate") is DataTemplate template)
        {
            contentControl.ContentTemplate = template;
        }
        else
        {
            var message = "CheckItemTemplate DataTemplateが見つかりません。Windowリソースに定義されているか確認してください。";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        return contentControl;
    }

    /// <summary>
    /// 階層の深さに応じた枠線の色を取得
    /// </summary>
    private Brush GetBorderBrush(int depth)
    {
        return depth switch
        {
            0 => new SolidColorBrush(Color.FromRgb(_settings.Colors.Depth0.R, _settings.Colors.Depth0.G, _settings.Colors.Depth0.B)),
            1 => new SolidColorBrush(Color.FromRgb(_settings.Colors.Depth1.R, _settings.Colors.Depth1.G, _settings.Colors.Depth1.B)),
            2 => new SolidColorBrush(Color.FromRgb(_settings.Colors.Depth2.R, _settings.Colors.Depth2.G, _settings.Colors.Depth2.B)),
            _ => new SolidColorBrush(Color.FromRgb(_settings.Colors.DepthDefault.R, _settings.Colors.DepthDefault.G, _settings.Colors.DepthDefault.B))
        };
    }

    /// <summary>
    /// キャプチャファイルの相対パスから絶対パスを解決する
    /// </summary>
    public string ResolveCaptureFilePath(string captureFilePath)
    {
        if (string.IsNullOrEmpty(captureFilePath))
        {
            throw new ArgumentNullException(nameof(captureFilePath));
        }

        var absolutePath = Path.Combine(_documentRootPath, captureFilePath);
        return Path.GetFullPath(absolutePath);
    }

    /// <summary>
    /// documentRootPathを取得する（テスト用）
    /// </summary>
    public string DocumentRootPath => _documentRootPath;
}
