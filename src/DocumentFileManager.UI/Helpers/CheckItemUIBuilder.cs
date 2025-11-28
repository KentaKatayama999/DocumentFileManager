using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Services.Abstractions;
using DocumentFileManager.UI.ViewModels;
using DocumentFileManager.UI.Windows;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Helpers;

/// <summary>
/// チェック項目のUI階層を動的に構築するヘルパークラス
/// Phase 4でリファクタリング：責務をUI構築とバインディング設定のみに限定
/// </summary>
public class CheckItemUIBuilder
{
    private readonly ICheckItemRepository _repository;
    private readonly ICheckItemDocumentRepository _checkItemDocumentRepository;
    private readonly IChecklistStateManager _stateManager;
    private readonly UISettings _settings;
    private readonly ILogger<CheckItemUIBuilder> _logger;
    private readonly string _documentRootPath;
    private Document? _currentDocument;
    private Func<CheckItemViewModel, UIElement, Task>? _onCaptureRequested;

    /// <summary>
    /// コンストラクタ（Phase 4: ChecklistStateManagerを追加）
    /// </summary>
    public CheckItemUIBuilder(
        ICheckItemRepository repository,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        IChecklistStateManager stateManager,
        UISettings settings,
        ILogger<CheckItemUIBuilder> logger,
        string documentRootPath)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _checkItemDocumentRepository = checkItemDocumentRepository ?? throw new ArgumentNullException(nameof(checkItemDocumentRepository));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _documentRootPath = documentRootPath ?? throw new ArgumentNullException(nameof(documentRootPath));
    }


    /// <summary>
    /// チェック項目の階層UIを構築する
    /// </summary>
    /// <param name="containerPanel">親となるPanel</param>
    /// <param name="document">紐づけるDocumentオブジェクト（nullの場合は全体表示）</param>
    /// <param name="onCaptureRequested">キャプチャ要求時に呼び出されるデリゲート</param>
    public async Task BuildAsync(Panel containerPanel, Document? document = null, Func<CheckItemViewModel, UIElement, Task>? onCaptureRequested = null)
    {
        _currentDocument = document;
        _onCaptureRequested = onCaptureRequested;

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
            // 特定の資料に紐づいたチェック項目を取得
            var linkedItems = await _checkItemDocumentRepository.GetByDocumentIdAsync(document.Id);
            checkItemDocuments = linkedItems.ToDictionary(x => x.CheckItemId);
            _logger.LogInformation("{Count} 件の紐づけデータを取得しました", linkedItems.Count);
        }
        else
        {
            // MainWindow（全体表示）の場合：各チェック項目の最新キャプチャを取得
            var allLinkedItems = await _checkItemDocumentRepository.GetAllAsync();

            // CheckItemIdでグループ化し、各グループ内でLinkedAtが最新のものを選択
            checkItemDocuments = allLinkedItems
                .Where(x => x.CaptureFile != null) // キャプチャがあるもののみ
                .GroupBy(x => x.CheckItemId)
                .Select(g => g.OrderByDescending(x => x.LinkedAt).First()) // 最新のもの
                .ToDictionary(x => x.CheckItemId);

            _logger.LogInformation("全体表示モード：{Count} 件のチェック項目に最新キャプチャがあります", checkItemDocuments.Count);
        }

        // ViewModelに変換
        var viewModels = BuildViewModelHierarchy(rootItems, checkItemDocuments);

        // UIを構築
        foreach (var viewModel in viewModels)
        {
            var groupBox = CreateGroupBox(viewModel, 0);

            // ルート項目の幅を設定（WrapPanelで横並び対応）
            // 内容に応じて自動調整されるため、最小幅のみ設定
            if (groupBox is GroupBox rootGroupBox)
            {
                rootGroupBox.MinWidth = _settings.GroupBox.RootMinWidth;
                // MaxWidthは設定せず、内容に応じて拡大できるようにする
            }

            containerPanel.Children.Add(groupBox);
        }

        _logger.LogInformation("チェック項目UIの構築が完了しました");
    }

    /// <summary>
    /// ViewModelの階層構造を構築する
    /// Phase 4: 拡張コンストラクタを使用し、コマンドを設定
    /// </summary>
    private List<CheckItemViewModel> BuildViewModelHierarchy(
        List<Entities.CheckItem> items,
        Dictionary<int, CheckItemDocument>? checkItemDocuments)
    {
        var viewModels = new List<CheckItemViewModel>();
        var isMainWindow = _currentDocument == null;

        foreach (var item in items)
        {
            // Phase 4: 拡張コンストラクタを使用
            var viewModel = new CheckItemViewModel(item, _documentRootPath, isMainWindow);

            // Documentと紐づいている場合は、紐づけデータからチェック状態を設定
            if (checkItemDocuments != null && checkItemDocuments.TryGetValue(item.Id, out var linkedItem))
            {
                if (_currentDocument != null)
                {
                    // ChecklistWindow（特定の資料）の場合：
                    // CaptureFileがある場合のみチェック済みとする（オフにしてもCaptureFileは維持される）
                    var hasCaptureFile = !string.IsNullOrEmpty(linkedItem.CaptureFile);
                    viewModel.IsChecked = hasCaptureFile;
                    viewModel.CaptureFilePath = linkedItem.CaptureFile;
                    _logger.LogDebug("紐づけデータからチェック状態を設定: {Path} = {IsChecked}, Capture={CaptureFile}",
                        item.Path, hasCaptureFile ? "チェック済み" : "未チェック", linkedItem.CaptureFile ?? "(なし)");
                }
                else
                {
                    // MainWindow（全体表示）の場合：最新のキャプチャのみ設定（チェック状態は設定しない）
                    viewModel.CaptureFilePath = linkedItem.CaptureFile;
                    _logger.LogDebug("最新キャプチャを設定: {Path}, Capture={CaptureFile}",
                        item.Path, linkedItem.CaptureFile ?? "(なし)");
                }
            }

            // Phase 4: コマンドを設定（ChecklistWindowの場合のみ）
            if (_currentDocument != null && viewModel.IsItem)
            {
                SetupCommands(viewModel);
            }

            // 子要素を再帰的に追加
            if (item.Children != null && item.Children.Count > 0)
            {
                var childViewModels = BuildViewModelHierarchy(item.Children.ToList(), checkItemDocuments);
                foreach (var child in childViewModels)
                {
                    viewModel.Children.Add(child);
                }
            }

            viewModels.Add(viewModel);
        }

        return viewModels;
    }

    /// <summary>
    /// ViewModelにコマンドを設定する
    /// Phase 4: ChecklistStateManagerを使用して状態遷移を管理
    /// </summary>
    private void SetupCommands(CheckItemViewModel viewModel)
    {
        // CheckedChangedCommand: チェック状態変更時の処理
        // 注意: このコマンドはChecked/Uncheckedイベントから直接呼ばれるのではなく、
        // IsCheckedのTwoWayバインディングによって状態が変わった後に明示的に実行される
        viewModel.CheckedChangedCommand = new AsyncRelayCommand(async () =>
        {
            if (_currentDocument == null)
            {
                _logger.LogWarning("Documentがnullのためチェック状態変更をスキップします");
                return;
            }

            try
            {
                if (viewModel.IsChecked)
                {
                    // チェックON処理
                    var transition = await _stateManager.HandleCheckOnAsync(viewModel, _currentDocument);

                    if (transition == null)
                    {
                        // キャンセルされた場合、チェック状態を戻す
                        viewModel.IsChecked = false;
                        _logger.LogInformation("チェックON操作がキャンセルされました");
                        return;
                    }

                    // 状態遷移をコミット
                    await _stateManager.CommitTransitionAsync(transition);

                    // ViewModelの状態を更新
                    viewModel.CaptureFilePath = transition.CaptureFile;
                    viewModel.UpdateCaptureButton();

                    // キャプチャ取得を促す（既存のキャプチャがない場合）
                    if (_onCaptureRequested != null && string.IsNullOrEmpty(transition.CaptureFile))
                    {
                        // キャプチャ取得は呼び出し元（ChecklistWindow）で処理
                        // ここではイベントを発火するのみ
                        _logger.LogDebug("キャプチャ取得可能状態: CheckItemId={CheckItemId}", viewModel.Id);
                    }
                }
                else
                {
                    // チェックOFF処理
                    var transition = await _stateManager.HandleCheckOffAsync(viewModel, _currentDocument);

                    // 状態遷移をコミット
                    await _stateManager.CommitTransitionAsync(transition);

                    // ViewModelの状態を更新
                    viewModel.UpdateCaptureButton();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "チェック状態変更中にエラーが発生しました: CheckItemId={CheckItemId}", viewModel.Id);
                // ロールバック: チェック状態を戻す
                viewModel.IsChecked = !viewModel.IsChecked;
                throw;
            }
        });

        // ViewCaptureCommand: キャプチャ表示
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
            bool? result = viewer.ShowDialog();

            // 削除された場合はViewModelを更新
            if (viewer.IsDeleted)
            {
                viewModel.CaptureFilePath = null;
                viewModel.UpdateCaptureButton();

                // DB更新（非同期処理を同期的に実行）
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
    /// GroupBoxまたはCheckBoxを作成する
    /// </summary>
    /// <param name="viewModel">ViewModel</param>
    /// <param name="depth">階層の深さ（インデント用）</param>
    private UIElement CreateGroupBox(CheckItemViewModel viewModel, int depth)
    {
        if (viewModel.IsItem)
        {
            // チェック項目の場合はCheckBoxを作成
            return CreateCheckBox(viewModel, depth);
        }
        else
        {
            // 子要素がチェック項目のみかどうかを判定
            var allChildrenAreItems = viewModel.Children.All(c => c.IsItem);
            var allChildrenAreCategories = viewModel.Children.All(c => c.IsCategory);
            var childCount = viewModel.Children.Count;

            // 分類の場合はGroupBoxを作成
            var groupBox = new GroupBox
            {
                Header = viewModel.Label,
                Margin = new Thickness(
                    depth * _settings.GroupBox.MarginDepthMultiplier,
                    _settings.GroupBox.MarginTop,
                    _settings.GroupBox.MarginRight,
                    _settings.GroupBox.MarginBottom),
                Padding = new Thickness(_settings.GroupBox.Padding),
                // チェックボックスを含むGroupBoxは常に小分類（Depth2）の色を使用
                BorderBrush = allChildrenAreItems ? GetBorderBrush(2) : GetBorderBrush(depth),
                BorderThickness = new Thickness(_settings.GroupBox.BorderThickness)
            };

            Panel containerPanel;
            bool isWrapPanel = false;

            // チェック項目が指定個数以上、または分類が指定個数以上の場合はWrapPanelで複数列表示
            if ((allChildrenAreItems && childCount >= _settings.Layout.WrapPanelItemThreshold) ||
                (allChildrenAreCategories && childCount >= _settings.Layout.WrapPanelCategoryThreshold))
            {
                containerPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal
                };
                isWrapPanel = true;
            }
            else
            {
                containerPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical
                };
            }

            // 子要素を再帰的に追加
            foreach (var child in viewModel.Children)
            {
                var childElement = CreateGroupBox(child, depth + 1);

                // WrapPanelの場合は幅を設定
                if (isWrapPanel)
                {
                    if (childElement is CheckBox checkBox)
                    {
                        // チェックボックスは内容に合わせて自動調整（最小幅のみ設定）
                        checkBox.MinWidth = _settings.CheckBox.MinWidth;
                        checkBox.HorizontalAlignment = HorizontalAlignment.Left;
                    }
                    else if (childElement is GroupBox childGroupBox)
                    {
                        childGroupBox.MinWidth = _settings.GroupBox.ChildItemMinWidth; // GroupBoxの最小幅
                    }
                }

                containerPanel.Children.Add(childElement);
            }

            // GroupBox自体の幅を設定（チェック項目が多い場合は内容に応じて自動調整）
            if (isWrapPanel && allChildrenAreItems)
            {
                // チェック項目の数に応じて最適な列数を計算
                int columnsPerRow = Math.Min(_settings.Layout.MaxColumnsPerRow, (childCount + 1) / 2);

                // 必要な幅を計算（余裕を持たせる）
                double calculatedWidth = columnsPerRow * _settings.Layout.WidthPerColumn + _settings.Layout.GroupBoxExtraPadding;

                // 最小幅を設定、最大幅は制限しない（内容に応じて拡大）
                groupBox.MinWidth = Math.Min(calculatedWidth, _settings.Layout.MaxCalculatedWidth);
                // 内容に応じて幅が自動調整されるようにMaxWidthは設定しない
            }
            else if (isWrapPanel && allChildrenAreCategories)
            {
                // 分類GroupBoxの場合も内容に応じて調整
                groupBox.MinWidth = _settings.GroupBox.ChildCategoryMinWidth;
            }

            groupBox.Content = containerPanel;
            return groupBox;
        }
    }

    /// <summary>
    /// CheckBoxと画像確認ボタンを含むStackPanelを作成する
    /// Phase 4: イベントハンドラをバインディングに置き換え
    /// </summary>
    private UIElement CreateCheckBox(CheckItemViewModel viewModel, int depth)
    {
        var checkBox = new CheckBox
        {
            Content = viewModel.Label,
            Margin = new Thickness(
                depth * _settings.CheckBox.MarginDepthMultiplier + _settings.CheckBox.MarginLeft,
                _settings.CheckBox.MarginTop,
                _settings.CheckBox.MarginRight,
                _settings.CheckBox.MarginBottom),
            FontSize = _settings.CheckBox.FontSize,
            DataContext = viewModel
        };

        // Phase 4: IsCheckedをTwoWayバインディング
        var isCheckedBinding = new Binding(nameof(CheckItemViewModel.IsChecked))
        {
            Source = viewModel,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        checkBox.SetBinding(CheckBox.IsCheckedProperty, isCheckedBinding);

        // Phase 4: IsEnabledをバインディング（MainWindowモードでは無効）
        var isEnabledBinding = new Binding(nameof(CheckItemViewModel.IsCheckBoxEnabled))
        {
            Source = viewModel
        };
        checkBox.SetBinding(CheckBox.IsEnabledProperty, isEnabledBinding);

        // 画像確認ボタン（カメラ絵文字）
        var imageButton = new Button
        {
            Content = "📷",
            Width = 24,
            Height = 20,
            Margin = new Thickness(5, 0, 0, 0),
            FontSize = 11,
            Background = new SolidColorBrush(Color.FromRgb(255, 220, 220)), // 薄い赤
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 160, 160)), // 薄い赤茶
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand, // ホバー時に手のカーソル
            Padding = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            DataContext = viewModel
        };

        // Phase 4: Visibilityをバインディング
        var visibilityBinding = new Binding(nameof(CheckItemViewModel.CameraButtonVisibility))
        {
            Source = viewModel
        };
        imageButton.SetBinding(Button.VisibilityProperty, visibilityBinding);

        // Phase 4: Commandをバインディング
        var commandBinding = new Binding(nameof(CheckItemViewModel.ViewCaptureCommand))
        {
            Source = viewModel
        };
        imageButton.SetBinding(Button.CommandProperty, commandBinding);

        // StackPanelにCheckBoxとボタンを配置
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Tag = new { CheckBox = checkBox, ImageButton = imageButton, ViewModel = viewModel }
        };
        stackPanel.Children.Add(checkBox);
        stackPanel.Children.Add(imageButton);

        // Phase 4: チェック状態変更時にコマンドを実行
        // 注意: TwoWayバインディングでIsCheckedが更新された後に、明示的にコマンドを実行
        checkBox.Checked += async (sender, e) =>
        {
            // MainWindowモードの場合は何もしない（IsCheckBoxEnabled=falseで操作できない）
            if (viewModel.IsMainWindow)
            {
                return;
            }

            // コマンドが設定されている場合は実行
            if (viewModel.CheckedChangedCommand?.CanExecute(null) == true)
            {
                viewModel.CheckedChangedCommand.Execute(null);

                // コマンド実行後、キャプチャ取得を促す
                if (_onCaptureRequested != null && !viewModel.HasCapture)
                {
                    var result = MessageBox.Show(
                        "この箇所のキャプチャを取得しますか？",
                        "キャプチャ確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await _onCaptureRequested(viewModel, stackPanel);
                    }
                }
            }
        };

        checkBox.Unchecked += (sender, e) =>
        {
            // MainWindowモードの場合は何もしない
            if (viewModel.IsMainWindow)
            {
                return;
            }

            // コマンドが設定されている場合は実行
            if (viewModel.CheckedChangedCommand?.CanExecute(null) == true)
            {
                viewModel.CheckedChangedCommand.Execute(null);
            }
        };

        return stackPanel;
    }

    // Phase 4: SaveStatusAsyncメソッドは削除されました
    // チェック状態の保存は ChecklistStateManager.CommitTransitionAsync に移行

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
    /// <param name="captureFilePath">キャプチャファイルの相対パス</param>
    /// <returns>絶対パス</returns>
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
