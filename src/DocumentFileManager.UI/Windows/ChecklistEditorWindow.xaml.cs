using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DocumentFileManager.Infrastructure.Models;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocumentFileManager.UI.Windows;

/// <summary>
/// チェックリストエディターウィンドウ
/// </summary>
public partial class ChecklistEditorWindow : Window
{
    private readonly ILogger<ChecklistEditorWindow> _logger;
    private readonly string _projectRoot;
    private readonly PathSettings _pathSettings;
    private string? _currentFilePath;
    private ObservableCollection<CheckItemEditorViewModel> _rootItems = new();

    public ChecklistEditorWindow(ILogger<ChecklistEditorWindow> logger, PathSettings pathSettings, string projectRoot)
    {
        _logger = logger;
        _projectRoot = projectRoot;
        _pathSettings = pathSettings;

        InitializeComponent();

        ChecklistTreeView.ItemsSource = _rootItems;

        _logger.LogInformation("チェックリストエディターを起動しました");
    }

    /// <summary>
    /// 読み込みボタンクリック
    /// </summary>
    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 共用フォルダが設定されていればそれを使用、なければプロジェクトルート
            var initialDirectory = string.IsNullOrEmpty(_pathSettings.ChecklistDefinitionsFolder)
                ? _projectRoot
                : _pathSettings.ChecklistDefinitionsFolder;

            var dialog = new OpenFileDialog
            {
                Title = "チェックリストファイルを選択",
                Filter = "JSONファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                InitialDirectory = initialDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                LoadChecklistFile(dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックリストの読み込みに失敗しました");
            MessageBox.Show($"チェックリストの読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// チェックリストファイルを読み込み
    /// </summary>
    private void LoadChecklistFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var root = JsonSerializer.Deserialize<ChecklistRoot>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (root == null || root.CheckItems == null)
        {
            throw new Exception("チェックリストファイルの形式が不正です");
        }

        _rootItems.Clear();
        foreach (var item in root.CheckItems)
        {
            _rootItems.Add(ConvertToViewModel(item));
        }

        _currentFilePath = filePath;
        StatusText.Text = $"読み込み完了: {Path.GetFileName(filePath)}";
        _logger.LogInformation("チェックリストを読み込みました: {FilePath}", filePath);
    }

    /// <summary>
    /// CheckItemDefinitionをViewModelに変換
    /// </summary>
    private CheckItemEditorViewModel ConvertToViewModel(CheckItemDefinition definition)
    {
        var viewModel = new CheckItemEditorViewModel
        {
            Label = definition.Label,
            Type = definition.Type,
            Checked = definition.Checked
        };

        if (definition.Children != null)
        {
            foreach (var child in definition.Children)
            {
                viewModel.Children.Add(ConvertToViewModel(child));
            }
        }

        return viewModel;
    }

    /// <summary>
    /// 保存ボタンクリック
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? filePath = _currentFilePath;

            if (string.IsNullOrEmpty(filePath))
            {
                // 共用フォルダが設定されていればそれを使用、なければプロジェクトルート
                var initialDirectory = string.IsNullOrEmpty(_pathSettings.ChecklistDefinitionsFolder)
                    ? _projectRoot
                    : _pathSettings.ChecklistDefinitionsFolder;

                var dialog = new SaveFileDialog
                {
                    Title = "チェックリストファイルを保存",
                    Filter = "JSONファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    InitialDirectory = initialDirectory,
                    FileName = "checklist.json"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                filePath = dialog.FileName;
            }

            SaveChecklistFile(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックリストの保存に失敗しました");
            MessageBox.Show($"チェックリストの保存に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// チェックリストファイルに保存
    /// </summary>
    private void SaveChecklistFile(string filePath)
    {
        var root = new ChecklistRoot
        {
            CheckItems = _rootItems.Select(ConvertToDefinition).ToList()
        };

        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        File.WriteAllText(filePath, json);

        _currentFilePath = filePath;
        StatusText.Text = $"保存完了: {Path.GetFileName(filePath)}";
        _logger.LogInformation("チェックリストを保存しました: {FilePath}", filePath);

        MessageBox.Show($"チェックリストを保存しました。\n\n{filePath}", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// ViewModelをCheckItemDefinitionに変換
    /// </summary>
    private CheckItemDefinition ConvertToDefinition(CheckItemEditorViewModel viewModel)
    {
        var definition = new CheckItemDefinition
        {
            Label = viewModel.Label,
            Type = viewModel.Type,
            Checked = viewModel.Checked
        };

        if (viewModel.Children.Count > 0)
        {
            definition.Children = viewModel.Children.Select(ConvertToDefinition).ToList();
        }

        return definition;
    }

    /// <summary>
    /// カテゴリ追加ボタンクリック
    /// </summary>
    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        var newCategory = new CheckItemEditorViewModel
        {
            Label = "新しいカテゴリ",
            Type = "category",
            Checked = false
        };

        if (selected == null)
        {
            // ルートに追加
            _rootItems.Add(newCategory);
        }
        else
        {
            // 選択項目の下に追加
            selected.Children.Add(newCategory);
        }

        StatusText.Text = "カテゴリを追加しました";
    }

    /// <summary>
    /// 項目追加ボタンクリック
    /// </summary>
    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        var newItem = new CheckItemEditorViewModel
        {
            Label = "新しい項目",
            Type = "item",
            Checked = false
        };

        if (selected == null)
        {
            // ルートに追加
            _rootItems.Add(newItem);
        }
        else
        {
            // 選択項目の下に追加
            selected.Children.Add(newItem);
        }

        StatusText.Text = "項目を追加しました";
    }

    /// <summary>
    /// 削除ボタンクリック
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null)
        {
            MessageBox.Show("削除する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"「{selected.Label}」を削除しますか？\n（子要素も削除されます）",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            RemoveItem(selected);
            StatusText.Text = $"「{selected.Label}」を削除しました";
        }
    }

    /// <summary>
    /// 項目を削除
    /// </summary>
    private void RemoveItem(CheckItemEditorViewModel item)
    {
        // ルートから探す
        if (_rootItems.Contains(item))
        {
            _rootItems.Remove(item);
            return;
        }

        // 再帰的に探す
        RemoveItemRecursive(_rootItems, item);
    }

    /// <summary>
    /// 項目を再帰的に削除
    /// </summary>
    private bool RemoveItemRecursive(ObservableCollection<CheckItemEditorViewModel> collection, CheckItemEditorViewModel target)
    {
        if (collection.Contains(target))
        {
            collection.Remove(target);
            return true;
        }

        foreach (var item in collection)
        {
            if (RemoveItemRecursive(item.Children, target))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 上へ移動ボタンクリック
    /// </summary>
    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null) return;

        MoveItem(selected, -1);
    }

    /// <summary>
    /// 下へ移動ボタンクリック
    /// </summary>
    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null) return;

        MoveItem(selected, 1);
    }

    /// <summary>
    /// 項目を移動
    /// </summary>
    private void MoveItem(CheckItemEditorViewModel item, int direction)
    {
        var parent = FindParent(item);
        var collection = parent?.Children ?? _rootItems;

        var index = collection.IndexOf(item);
        if (index < 0) return;

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= collection.Count) return;

        collection.Move(index, newIndex);
        StatusText.Text = "項目を移動しました";
    }

    /// <summary>
    /// 親項目を検索
    /// </summary>
    private CheckItemEditorViewModel? FindParent(CheckItemEditorViewModel target)
    {
        return FindParentRecursive(_rootItems, target);
    }

    /// <summary>
    /// 親項目を再帰的に検索
    /// </summary>
    private CheckItemEditorViewModel? FindParentRecursive(ObservableCollection<CheckItemEditorViewModel> collection, CheckItemEditorViewModel target)
    {
        foreach (var item in collection)
        {
            if (item.Children.Contains(target))
            {
                return item;
            }

            var found = FindParentRecursive(item.Children, target);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// TreeViewの選択項目変更
    /// </summary>
    private void ChecklistTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null) return;

        // プロパティエディタに反映
        LabelTextBox.Text = selected.Label;
        TypeComboBox.SelectedIndex = selected.Type == "category" ? 0 : 1;
        CheckedCheckBox.IsChecked = selected.Checked;
        CheckedCheckBox.IsEnabled = selected.Type == "item";

        // 情報表示
        var childCount = selected.Children.Count;
        InfoTextBlock.Text = $"子要素: {childCount} 個\n種別: {selected.TypeLabel}";
    }

    /// <summary>
    /// ラベルテキスト変更
    /// </summary>
    private void LabelTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null) return;

        selected.Label = LabelTextBox.Text;
    }

    /// <summary>
    /// 種別コンボボックス変更
    /// </summary>
    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null) return;

        var selectedItem = TypeComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem == null) return;

        selected.Type = selectedItem.Tag?.ToString() ?? "category";
        CheckedCheckBox.IsEnabled = selected.Type == "item";
    }

    /// <summary>
    /// チェック状態変更
    /// </summary>
    private void CheckedCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        var selected = ChecklistTreeView.SelectedItem as CheckItemEditorViewModel;
        if (selected == null) return;

        selected.Checked = CheckedCheckBox.IsChecked == true;
    }
}
