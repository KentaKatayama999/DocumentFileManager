using System.IO;
using System.Windows;
using System.Windows.Input;

namespace DocumentFileManager.UI;

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

    public ChecklistSelectionDialog(string projectRoot)
    {
        InitializeComponent();
        _projectRoot = projectRoot;

        LoadChecklistFiles();
    }

    /// <summary>
    /// チェックリストファイルを読み込む
    /// </summary>
    private void LoadChecklistFiles()
    {
        var checklistFiles = new List<ChecklistFileInfo>();

        // プロジェクトルートから checklist*.json を検索
        var pattern = "checklist*.json";
        if (Directory.Exists(_projectRoot))
        {
            var files = Directory.GetFiles(_projectRoot, pattern, SearchOption.TopDirectoryOnly);

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
                $"チェックリストファイルが見つかりません。\n\nプロジェクトルート: {_projectRoot}\nパターン: {pattern}",
                "警告",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // デフォルトのchecklist.jsonを使う
            checklistFiles.Add(new ChecklistFileInfo
            {
                FilePath = Path.Combine(_projectRoot, "checklist.json"),
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
