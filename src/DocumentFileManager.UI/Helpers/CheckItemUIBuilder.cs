using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private readonly UISettings _settings;
    private readonly ILogger<CheckItemUIBuilder> _logger;

    public CheckItemUIBuilder(ICheckItemRepository repository, UISettings settings, ILogger<CheckItemUIBuilder> logger)
    {
        _repository = repository;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// チェック項目の階層UIを構築する
    /// </summary>
    /// <param name="containerPanel">親となるPanel</param>
    public async Task BuildAsync(Panel containerPanel)
    {
        _logger.LogInformation("チェック項目UIの構築を開始します");

        containerPanel.Children.Clear();

        // ルート項目を取得
        var rootItems = await _repository.GetRootItemsAsync();

        _logger.LogInformation("{Count} 件のルート項目を取得しました", rootItems.Count);

        // ViewModelに変換
        var viewModels = BuildViewModelHierarchy(rootItems);

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
    private List<CheckItemViewModel> BuildViewModelHierarchy(List<Entities.CheckItem> items)
    {
        var viewModels = new List<CheckItemViewModel>();

        foreach (var item in items)
        {
            var viewModel = new CheckItemViewModel(item);

            // 子要素を再帰的に追加
            if (item.Children != null && item.Children.Count > 0)
            {
                var childViewModels = BuildViewModelHierarchy(item.Children.ToList());
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
                BorderBrush = GetBorderBrush(depth),
                BorderThickness = new Thickness(_settings.GroupBox.BorderThickness)
            };

            // 子要素がチェック項目のみかどうかを判定
            var allChildrenAreItems = viewModel.Children.All(c => c.IsItem);
            var allChildrenAreCategories = viewModel.Children.All(c => c.IsCategory);
            var childCount = viewModel.Children.Count;

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
    /// CheckBoxを作成する
    /// </summary>
    private CheckBox CreateCheckBox(CheckItemViewModel viewModel, int depth)
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

        // チェック状態変更イベント
        checkBox.Checked += async (sender, e) =>
        {
            viewModel.IsChecked = true;
            await SaveStatusAsync(viewModel);
        };

        checkBox.Unchecked += async (sender, e) =>
        {
            viewModel.IsChecked = false;
            await SaveStatusAsync(viewModel);
        };

        return checkBox;
    }

    /// <summary>
    /// チェック状態をDBに保存する
    /// </summary>
    private async Task SaveStatusAsync(CheckItemViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("チェック状態を保存: {Path} = {Status}", viewModel.Path, viewModel.Status);

            await _repository.UpdateAsync(viewModel.Entity);
            await _repository.SaveChangesAsync();

            _logger.LogDebug("チェック状態の保存が完了しました");
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
