using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DocumentFileManager.UI.Configuration;

namespace DocumentFileManager.UI.Dialogs;

public partial class ChecklistSelectionDialog : Window
{
    public class ChecklistFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public string? SelectedChecklistFileName { get; private set; }
    public string? SelectedChecklistFilePath { get; private set; }

    private readonly string _projectRoot;
    private readonly PathSettings? _pathSettings;

    public ChecklistSelectionDialog(string projectRoot, PathSettings? pathSettings = null)
    {
        InitializeComponent();
        _projectRoot = projectRoot;
        _pathSettings = pathSettings;

        LoadChecklistFiles();
    }

    private void LoadChecklistFiles()
    {
        var checklistFiles = new List<ChecklistFileInfo>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const string pattern = "checklist*.json";

        var folders = EnumerateCandidateFolders().ToList();

        foreach (var folder in folders.Where(Directory.Exists))
        {
            foreach (var file in Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly))
            {
                if (!seenPaths.Add(file))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);
                var displayName = fileName
                    .Replace("checklist", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(".json", "", StringComparison.OrdinalIgnoreCase)
                    .Trim('_', '-', ' ');

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = "標準";
                }

                checklistFiles.Add(new ChecklistFileInfo
                {
                    FilePath = file,
                    FileName = fileName,
                    DisplayName = $"{displayName} ({fileName})"
                });
            }
        }

        if (!checklistFiles.Any())
        {
            var fallbackFolder = folders.FirstOrDefault() ?? _projectRoot;
            MessageBox.Show(
                $"チェックリストファイルが見つかりませんでした。\n\n検索フォルダ: {fallbackFolder}\n検索パターン: {pattern}",
                "注意",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            checklistFiles.Add(new ChecklistFileInfo
            {
                FilePath = Path.Combine(fallbackFolder, "checklist.json"),
                FileName = "checklist.json",
                DisplayName = "標準 (checklist.json)"
            });
        }

        ChecklistListBox.ItemsSource = checklistFiles;

        if (ChecklistListBox.Items.Count > 0)
        {
            ChecklistListBox.SelectedIndex = 0;
        }
    }

    private IEnumerable<string> EnumerateCandidateFolders()
    {
        if (_pathSettings != null)
        {
            if (!string.IsNullOrWhiteSpace(_pathSettings.ConfigDirectory))
            {
                yield return _pathSettings.ToAbsolutePath(_projectRoot, _pathSettings.ConfigDirectory);
            }

            if (!string.IsNullOrWhiteSpace(_pathSettings.ChecklistDefinitionsFolder))
            {
                yield return _pathSettings.ToAbsolutePath(_projectRoot, _pathSettings.ChecklistDefinitionsFolder);
                yield break;
            }
        }

        yield return _projectRoot;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChecklistListBox.SelectedItem is ChecklistFileInfo selectedFile)
        {
            SelectedChecklistFileName = selectedFile.FileName;
            SelectedChecklistFilePath = selectedFile.FilePath;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("チェックリストを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ChecklistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ChecklistListBox.SelectedItem != null)
        {
            OkButton_Click(sender, e);
        }
    }
}
