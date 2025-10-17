using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Helpers;

/// <summary>
/// チェック項目のUI階層を動的に構築するヘルパークラス
/// </summary>
public class CheckItemUIBuilder
{
    private readonly ICheckItemRepository _repository;
    private readonly ICheckItemDocumentRepository _checkItemDocumentRepository;
    private readonly UISettings _settings;
    private readonly ILogger<CheckItemUIBuilder> _logger;
    private Document? _currentDocument;
    private Func<CheckItemViewModel, UIElement, Task>? _onCaptureRequested;

    public CheckItemUIBuilder(
        ICheckItemRepository repository,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        UISettings settings,
        ILogger<CheckItemUIBuilder> logger)
    {
        _repository = repository;
        _checkItemDocumentRepository = checkItemDocumentRepository;
        _settings = settings;
        _logger = logger;
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

        // Documentと紐づいたチェック項目を取得（Documentが指定されている場合）
        Dictionary<int, CheckItemDocument>? checkItemDocuments = null;
        if (document != null)
        {
            var linkedItems = await _checkItemDocumentRepository.GetByDocumentIdAsync(document.Id);
            checkItemDocuments = linkedItems.ToDictionary(x => x.CheckItemId);
            _logger.LogInformation("{Count} 件の紐づけデータを取得しました", linkedItems.Count);
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
    /// </summary>
    private List<CheckItemViewModel> BuildViewModelHierarchy(
        List<Entities.CheckItem> items,
        Dictionary<int, CheckItemDocument>? checkItemDocuments)
    {
        var viewModels = new List<CheckItemViewModel>();

        foreach (var item in items)
        {
            var viewModel = new CheckItemViewModel(item);

            // Documentと紐づいている場合は、紐づけデータからチェック状態を設定
            if (checkItemDocuments != null && checkItemDocuments.TryGetValue(item.Id, out var linkedItem))
            {
                viewModel.IsChecked = true; // 紐づけが存在する場合はチェック済みとする
                viewModel.CaptureFilePath = linkedItem.CaptureFile; // キャプチャファイルパスを設定
                _logger.LogDebug("紐づけデータからチェック状態を設定: {Path} = チェック済み, Capture={CaptureFile}",
                    item.Path, linkedItem.CaptureFile ?? "(なし)");
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
    /// </summary>
    private UIElement CreateCheckBox(CheckItemViewModel viewModel, int depth)
    {
        var checkBox = new CheckBox
        {
            Content = viewModel.Label,
            IsChecked = viewModel.IsChecked,
            Margin = new Thickness(
                depth * _settings.CheckBox.MarginDepthMultiplier + _settings.CheckBox.MarginLeft,
                _settings.CheckBox.MarginTop,
                _settings.CheckBox.MarginRight,
                _settings.CheckBox.MarginBottom),
            FontSize = _settings.CheckBox.FontSize,
            Tag = viewModel // ViewModelを保持
        };

        // 画像確認ボタン（カメラ絵文字）
        var imageButton = new Button
        {
            Content = "📷",
            Width = 24,
            Height = 20,
            Margin = new Thickness(5, 0, 0, 0),
            Visibility = viewModel.HasCapture ? Visibility.Visible : Visibility.Collapsed,
            Tag = viewModel, // ViewModelを保持
            FontSize = 11,
            Background = new SolidColorBrush(Color.FromRgb(255, 220, 220)), // 薄い赤
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 160, 160)), // 薄い赤茶
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand, // ホバー時に手のカーソル
            Padding = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        // 画像確認ボタンクリック
        imageButton.Click += (sender, e) =>
        {
            if (viewModel.CaptureFilePath != null && _currentDocument != null)
            {
                var absolutePath = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                    "..", "..", "..", "..", "..",
                    viewModel.CaptureFilePath);
                absolutePath = Path.GetFullPath(absolutePath);

                _logger.LogInformation("キャプチャ画像を表示: {Path}", absolutePath);

                var viewer = new CaptureImageViewerWindow(absolutePath, null);
                bool? result = viewer.ShowDialog();

                // 削除された場合はボタンを非表示にする
                if (viewer.IsDeleted)
                {
                    viewModel.CaptureFilePath = null;
                    imageButton.Visibility = Visibility.Collapsed;

                    // DBも更新（非同期処理を同期的に実行）
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
            }
        };

        // StackPanelにCheckBoxとボタンを配置
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Tag = new { CheckBox = checkBox, ImageButton = imageButton, ViewModel = viewModel }
        };
        stackPanel.Children.Add(checkBox);
        stackPanel.Children.Add(imageButton);

        // チェック状態変更イベント
        checkBox.Checked += async (sender, e) =>
        {
            viewModel.IsChecked = true;
            await SaveStatusAsync(viewModel);

            // Documentが指定されている場合、キャプチャを取得するか確認
            if (_currentDocument != null && _onCaptureRequested != null)
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
        };

        checkBox.Unchecked += async (sender, e) =>
        {
            viewModel.IsChecked = false;
            await SaveStatusAsync(viewModel);
        };

        return stackPanel;
    }

    /// <summary>
    /// チェック状態をDBに保存する（Documentと紐づけて保存）
    /// </summary>
    private async Task SaveStatusAsync(CheckItemViewModel viewModel)
    {
        try
        {
            if (_currentDocument == null)
            {
                // Documentが指定されていない場合は、CheckItemのStatusを更新
                _logger.LogInformation("チェック状態を保存: {Path} = {Status}", viewModel.Path, viewModel.Status);

                await _repository.UpdateAsync(viewModel.Entity);
                await _repository.SaveChangesAsync();

                _logger.LogDebug("チェック状態の保存が完了しました");
            }
            else
            {
                // Documentが指定されている場合は、CheckItemDocumentテーブルに保存
                if (viewModel.IsChecked)
                {
                    // チェックONの場合：CheckItemDocumentに追加（既に存在する場合は何もしない）
                    var existing = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                        _currentDocument.Id,
                        viewModel.Entity.Id);

                    if (existing == null)
                    {
                        var checkItemDocument = new CheckItemDocument
                        {
                            DocumentId = _currentDocument.Id,
                            CheckItemId = viewModel.Entity.Id,
                            LinkedAt = DateTime.UtcNow
                        };

                        await _checkItemDocumentRepository.AddAsync(checkItemDocument);
                        await _checkItemDocumentRepository.SaveChangesAsync();

                        _logger.LogInformation("チェック項目を資料に紐づけました: Document={DocumentId}, CheckItem={CheckItemId} ({Path})",
                            _currentDocument.Id, viewModel.Entity.Id, viewModel.Path);
                    }
                }
                else
                {
                    // チェックOFFの場合：CheckItemDocumentから削除
                    var existing = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                        _currentDocument.Id,
                        viewModel.Entity.Id);

                    if (existing != null)
                    {
                        await _checkItemDocumentRepository.DeleteAsync(existing.Id);
                        await _checkItemDocumentRepository.SaveChangesAsync();

                        _logger.LogInformation("チェック項目の紐づけを解除しました: Document={DocumentId}, CheckItem={CheckItemId} ({Path})",
                            _currentDocument.Id, viewModel.Entity.Id, viewModel.Path);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック状態の保存に失敗しました: {Path}", viewModel.Path);
        }
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
}
