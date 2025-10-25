using System.IO;
using System.Windows;
using System.Windows.Input;
using DocumentFileManager.UI.Configuration;

namespace DocumentFileManager.UI.Dialogs;

/// <summary>
/// チェックリスト選択ダイアログ
/// </summary>
public partial class ChecklistSelectionDialog : Window
{
    /// <summary>
    /// チェックリストファイル情報
    /// </summary>
    public class ChecklistFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 選択されたチェックリストファイル名
    /// </summary>
    public string? SelectedChecklistFileName { get; private set; }

    private readonly string _projectRoot;
    private readonly PathSettings? _pathSettings;

    public ChecklistSelectionDialog(string projectRoot, PathSettings? pathSettings = null)
    {
        InitializeComponent();
        _projectRoot = projectRoot;
        _pathSettings = pathSettings;

        LoadChecklistFiles();
    }

    /// <summary>
    /// チェックリストファイルを読み込む
    /// </summary>
    private void LoadChecklistFiles()
    {
        var checklistFiles = new List<ChecklistFileInfo>();

        // ChecklistDefinitionsFolder が設定されている場合はそちらを優先、未設定の場合は _projectRoot を使用
        var searchFolder = !string.IsNullOrEmpty(_pathSettings?.ChecklistDefinitionsFolder)
            ? _pathSettings.ChecklistDefinitionsFolder
            : _projectRoot;

        // チェックリストファイルを検索
        var pattern = "checklist*.json";
        if (Directory.Exists(searchFolder))
        {
            var files = Directory.GetFiles(searchFolder, pattern, SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var displayName = fileName.Replace("checklist", "").Replace(".json", "").Trim('_', '-');

                // ファイル名が "checklist.json" の場合は "デフォルト" と表示
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = "デフォルト";
                }

                checklistFiles.Add(new ChecklistFileInfo
                {
                    FilePath = file,
                    FileName = fileName,
                    DisplayName = $"{displayName} ({fileName})"
                });
            }
        }

        // デフォルトのchecklist.jsonがない場合は警告
        if (!checklistFiles.Any())
        {
            MessageBox.Show(
                $"チェックリストファイルが見つかりません。\n\n検索フォルダ: {searchFolder}\nパターン: {pattern}",
                "警告",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // デフォルトのchecklist.jsonを使う
            checklistFiles.Add(new ChecklistFileInfo
            {
                FilePath = Path.Combine(searchFolder, "checklist.json"),
                FileName = "checklist.json",
                DisplayName = "デフォルト (checklist.json)"
            });
        }

        ChecklistListBox.ItemsSource = checklistFiles;

        // 最初の項目を選択
        if (ChecklistListBox.Items.Count > 0)
        {
            ChecklistListBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// OKボタンクリック
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChecklistListBox.SelectedItem is ChecklistFileInfo selectedFile)
        {
            SelectedChecklistFileName = selectedFile.FileName;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("チェックリストを選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// キャンセルボタンクリック
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// リストボックスダブルクリック（OKボタンと同じ動作）
    /// </summary>
    private void ChecklistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ChecklistListBox.SelectedItem != null)
        {
            OkButton_Click(sender, e);
        }
    }
}
