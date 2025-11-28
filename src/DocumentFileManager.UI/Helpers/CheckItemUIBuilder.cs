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
using DocumentFileManager.UI.Windows;
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
    private readonly string _documentRootPath;
    private Document? _currentDocument;
    private Func<CheckItemViewModel, UIElement, Task>? _onCaptureRequested;

    public CheckItemUIBuilder(
        ICheckItemRepository repository,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        UISettings settings,
        ILogger<CheckItemUIBuilder> logger,
        string documentRootPath)
    {
        _repository = repository;
        _checkItemDocumentRepository = checkItemDocumentRepository;
        _settings = settings;
        _logger = logger;
        _documentRootPath = documentRootPath;
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
        // キャプチャがあり、かつファイルが実際に存在する場合のみ表示
        var captureFileExists = viewModel.HasCapture &&
            !string.IsNullOrEmpty(viewModel.CaptureFilePath) &&
            File.Exists(ResolveCaptureFilePath(viewModel.CaptureFilePath));

        var imageButton = new Button
        {
            Content = "📷",
            Width = 24,
            Height = 20,
            Margin = new Thickness(5, 0, 0, 0),
            Visibility = captureFileExists ? Visibility.Visible : Visibility.Collapsed,
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
            if (viewModel.CaptureFilePath != null)
            {
                var absolutePath = ResolveCaptureFilePath(viewModel.CaptureFilePath);

                _logger.LogInformation("キャプチャ画像を表示: {Path} (documentRootPath: {Root})", absolutePath, _documentRootPath);

                var viewer = new CaptureImageViewerWindow(absolutePath, null);
                bool? result = viewer.ShowDialog();

                // 削除された場合はボタンを非表示にする
                if (viewer.IsDeleted)
                {
                    viewModel.CaptureFilePath = null;
                    imageButton.Visibility = Visibility.Collapsed;

                    // DBも更新（非同期処理を同期的に実行）
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

        // チェック状態変更イベント（ChecklistWindowのみ有効）
        checkBox.Checked += async (sender, e) =>
        {
            // MainWindow（_currentDocument == null）ではチェック状態を元に戻して何もしない
            if (_currentDocument == null)
            {
                checkBox.IsChecked = viewModel.IsChecked;
                return;
            }

            // 既存の紐づき画像があるかチェック
            var existingLink = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                _currentDocument.Id, viewModel.Entity.Id);

            if (existingLink != null && !string.IsNullOrEmpty(existingLink.CaptureFile))
            {
                // 既存の画像がある場合、復帰するか確認
                var absolutePath = ResolveCaptureFilePath(existingLink.CaptureFile);
                if (File.Exists(absolutePath))
                {
                    var restoreResult = MessageBox.Show(
                        "以前保存したキャプチャ画像があります。復帰しますか？\n\n「いいえ」を選択すると破棄して新しく紐づけます。",
                        "画像復帰確認",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (restoreResult == MessageBoxResult.Cancel)
                    {
                        // キャンセル：チェックを元に戻す
                        checkBox.IsChecked = false;
                        return;
                    }
                    else if (restoreResult == MessageBoxResult.Yes)
                    {
                        // 復帰：既存の画像を使用
                        viewModel.IsChecked = true;
                        viewModel.CaptureFilePath = existingLink.CaptureFile;
                        imageButton.Visibility = Visibility.Visible;
                        // DBは既に紐づいているので更新不要
                        _logger.LogInformation("既存のキャプチャ画像を復帰: {Path}", existingLink.CaptureFile);
                        return;
                    }
                    // 「いいえ」の場合：既存のキャプチャを破棄して続行
                    await _checkItemDocumentRepository.UpdateCaptureFileAsync(existingLink.Id, null);
                    await _checkItemDocumentRepository.SaveChangesAsync();
                    viewModel.CaptureFilePath = null;
                    _logger.LogInformation("既存のキャプチャ画像を破棄: {Path}", existingLink.CaptureFile);
                }
            }

            viewModel.IsChecked = true;

            // 紐づけを作成（キャプチャの有無に関わらず）
            await SaveStatusAsync(viewModel);

            // キャプチャを取得するか確認
            if (_onCaptureRequested != null)
            {
                var result = MessageBox.Show(
                    "この箇所のキャプチャを取得しますか？",
                    "キャプチャ確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _onCaptureRequested(viewModel, stackPanel);
                    // UI更新は PerformCaptureForCheckItem 内で行われる
                }
            }

            // チェック状態を確実に反映（いいえを押した場合も含む）
            checkBox.IsChecked = true;
        };

        checkBox.Unchecked += async (sender, e) =>
        {
            // MainWindow（_currentDocument == null）ではチェック状態を元に戻して何もしない
            if (_currentDocument == null)
            {
                checkBox.IsChecked = viewModel.IsChecked;
                return;
            }

            viewModel.IsChecked = false;

            // カメラアイコンボタンを非表示にする
            imageButton.Visibility = Visibility.Collapsed;

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
                    // チェックONの場合：CheckItemDocumentに追加または更新
                    var existing = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                        _currentDocument.Id,
                        viewModel.Entity.Id);

                    if (existing == null)
                    {
                        // 新規作成
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
                    else
                    {
                        // 既存の紐づきがある場合は LinkedAt を更新（上書き）
                        existing.LinkedAt = DateTime.UtcNow;
                        await _checkItemDocumentRepository.UpdateAsync(existing);
                        await _checkItemDocumentRepository.SaveChangesAsync();

                        _logger.LogInformation("チェック項目の紐づけを更新しました: Document={DocumentId}, CheckItem={CheckItemId} ({Path})",
                            _currentDocument.Id, viewModel.Entity.Id, viewModel.Path);
                    }
                }
                else
                {
                    // チェックOFFの場合：紐づきは削除せず維持する（再度オンにしたときに復帰できるように）
                    // UIの表示状態のみ変更（カメラアイコンは非表示になる）
                    _logger.LogInformation("チェック項目をオフにしました（紐づきは維持）: Document={DocumentId}, CheckItem={CheckItemId} ({Path})",
                        _currentDocument.Id, viewModel.Entity.Id, viewModel.Path);
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
