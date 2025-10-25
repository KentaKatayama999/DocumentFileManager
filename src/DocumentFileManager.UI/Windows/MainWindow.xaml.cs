using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Dialogs;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocumentFileManager.UI.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ICheckItemRepository _checkItemRepository;
    private readonly CheckItemUIBuilder _checkItemUIBuilder;
    private readonly UISettings _uiSettings;
    private readonly PathSettings _pathSettings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindow> _logger;
    private readonly IDataIntegrityService _dataIntegrityService;
    private readonly string _documentRootPath;

    // チェックリストウィンドウ（シングルトン管理）
    private ChecklistWindow? _checklistWindow;
    private IntPtr _lastOpenedDocumentWindowHandle = IntPtr.Zero;

    // フィルタリングとハイライト用
    private List<Document> _allDocuments = new List<Document>();
    private CheckItemViewModel? _selectedCheckItem;
    private Dictionary<int, StackPanel> _checkItemUIElements = new Dictionary<int, StackPanel>();

    public MainWindow(
        IDocumentRepository documentRepository,
        ICheckItemRepository checkItemRepository,
        CheckItemUIBuilder checkItemUIBuilder,
        UISettings uiSettings,
        PathSettings pathSettings,
        IServiceProvider serviceProvider,
        IDataIntegrityService dataIntegrityService,
        ILogger<MainWindow> logger,
        string documentRootPath)
    {
        _documentRepository = documentRepository;
        _checkItemRepository = checkItemRepository;
        _checkItemUIBuilder = checkItemUIBuilder;
        _uiSettings = uiSettings;
        _pathSettings = pathSettings;
        _serviceProvider = serviceProvider;
        _dataIntegrityService = dataIntegrityService;
        _logger = logger;
        _documentRootPath = documentRootPath;

        InitializeComponent();

        // ウィンドウが読み込まれたときに自動読み込み
        Loaded += Window_Loaded;

        _logger.LogInformation("MainWindow が初期化されました");
    }

    /// <summary>
    /// ウィンドウが読み込まれたときの処理
    /// </summary>
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // プロジェクトルートを取得
            var projectRoot = GetProjectRoot();

            // チェックリストファイルが設定されているか、ファイルが存在するかをチェック
            var checklistFilePath = Path.Combine(projectRoot, _pathSettings.SelectedChecklistFile);
            var shouldShowDialog = string.IsNullOrEmpty(_pathSettings.SelectedChecklistFile) ||
                                    !File.Exists(checklistFilePath);

            if (shouldShowDialog)
            {
                _logger.LogInformation("Window_Loaded: チェックリスト選択ダイアログを表示します（初回起動または設定が無効）");

                // チェックリスト選択ダイアログを表示
                var selectionDialog = new ChecklistSelectionDialog(projectRoot, _pathSettings)
                {
                    Owner = this
                };
                var dialogResult = selectionDialog.ShowDialog();

                if (dialogResult != true || string.IsNullOrEmpty(selectionDialog.SelectedChecklistFileName))
                {
                    _logger.LogWarning("チェックリストが選択されませんでした。デフォルトのチェックリストを使用します。");
                }
                else
                {
                    // 選択されたチェックリストファイル名をPathSettingsに設定
                    _pathSettings.SelectedChecklistFile = selectionDialog.SelectedChecklistFileName;
                    _logger.LogInformation("選択されたチェックリスト: {FileName}", _pathSettings.SelectedChecklistFile);

                    // 設定を保存
                    var settingsPersistence = _serviceProvider.GetRequiredService<Services.SettingsPersistence>();
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var appsettingsPath = Path.Combine(baseDirectory, "appsettings.json");
                    await settingsPersistence.SavePathSettingsAsync(_pathSettings, appsettingsPath);
                    _logger.LogInformation("設定を保存しました");
                }
            }
            else
            {
                _logger.LogInformation("Window_Loaded: 既存のチェックリスト設定を使用します: {FileName}", _pathSettings.SelectedChecklistFile);
            }

            _logger.LogInformation("Window_Loaded: 自動読み込みを開始します");
            await LoadDocumentsAsync();
            _logger.LogInformation("Window_Loaded: 資料の読み込みが完了しました");
            await LoadCheckItemsAsync();
            _logger.LogInformation("Window_Loaded: チェック項目の読み込みが完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Window_Loaded: 処理でエラーが発生しました");
            MessageBox.Show($"処理でエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 資料を読み込む
    /// </summary>
    private async Task LoadDocumentsAsync()
    {
        try
        {
            _logger.LogInformation("資料の読み込みを開始します");
            StatusText.Text = "資料を読み込み中...";

            var documents = await _documentRepository.GetAllAsync();
            _allDocuments = documents; // 全資料リストを保存
            DocumentsListView.ItemsSource = documents;
            DocumentCountText.Text = $"{documents.Count} 件";

            _logger.LogInformation("資料を {Count} 件読み込みました", documents.Count);
            StatusText.Text = "資料の読み込みが完了しました";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料の読み込みに失敗しました");
            StatusText.Text = $"エラー: {ex.Message}";
            MessageBox.Show($"資料の読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// チェック項目を読み込む
    /// </summary>
    private async Task LoadCheckItemsAsync()
    {
        try
        {
            _logger.LogInformation("チェック項目の読み込みを開始します");
            StatusText.Text = "チェック項目を読み込み中...";

            // UIBuilderを使用してGroupBox階層を構築
            await _checkItemUIBuilder.BuildAsync(CheckItemsContainer);

            // チェックボックスのイベントを登録
            RegisterCheckBoxEvents(CheckItemsContainer);

            // ルート項目の数を取得して表示
            var rootItems = await _checkItemRepository.GetRootItemsAsync();
            CheckItemCountText.Text = $"{rootItems.Count} 件（ルート項目）";

            _logger.LogInformation("チェック項目の階層表示が完了しました（ルート項目: {Count} 件）", rootItems.Count);
            StatusText.Text = "チェック項目の読み込みが完了しました";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック項目の読み込みに失敗しました");
            StatusText.Text = $"エラー: {ex.Message}";
            MessageBox.Show($"チェック項目の読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("設定ウィンドウを開きます");

            // SettingsWindowをDIコンテナから取得
            var pathSettings = _serviceProvider.GetRequiredService<PathSettings>();
            var settingsLogger = _serviceProvider.GetRequiredService<ILogger<SettingsWindow>>();
            var settingsWindow = new SettingsWindow(_uiSettings, pathSettings, settingsLogger, _documentRootPath)
            {
                Owner = this
            };

            var result = settingsWindow.ShowDialog();

            if (result == true)
            {
                _logger.LogInformation("設定が保存されました");
                StatusText.Text = "設定を保存しました（再起動後に反映されます）";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ウィンドウの表示に失敗しました");
            MessageBox.Show($"設定ウィンドウの表示に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// チェックリストエディターメニュークリック
    /// </summary>
    private void ChecklistEditorMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("チェックリストエディターを開きます");

            var editorLogger = _serviceProvider.GetRequiredService<ILogger<ChecklistEditorWindow>>();
            var pathSettings = _serviceProvider.GetRequiredService<PathSettings>();
            var editorWindow = new ChecklistEditorWindow(editorLogger, pathSettings, GetProjectRoot())
            {
                Owner = this
            };

            editorWindow.ShowDialog();

            _logger.LogInformation("チェックリストエディターを閉じました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックリストエディターの表示に失敗しました");
            MessageBox.Show($"チェックリストエディターの表示に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 新規チェックリスト作成メニュークリック
    /// </summary>
    private async void CreateNewChecklistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("新規チェックリスト作成ダイアログを開きます");

            // ファイル名入力ダイアログを表示
            var inputDialog = new Window
            {
                Title = "新規チェックリスト作成",
                Width = 450,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "新しいチェックリスト名を入力してください:",
                Margin = new Thickness(0, 0, 0, 10)
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = "（例: 建築プロジェクト、設備点検など）",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 20) };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "作成", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            var cancelButton = new Button { Content = "キャンセル", Width = 80, IsCancel = true };

            okButton.Click += (s, args) => { inputDialog.DialogResult = true; inputDialog.Close(); };
            cancelButton.Click += (s, args) => { inputDialog.DialogResult = false; inputDialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            inputDialog.Content = stackPanel;
            textBox.Focus();

            bool? result = inputDialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var checklistName = textBox.Text.Trim();
                _logger.LogInformation("新規チェックリストを作成: {ChecklistName}", checklistName);

                // ファイル名を生成（checklist_xxx.json形式）
                var safeFileName = string.Concat(checklistName.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"checklist_{safeFileName}.json";

                var projectRoot = GetProjectRoot();
                var filePath = Path.Combine(projectRoot, fileName);

                // 既に同名のファイルが存在する場合は確認
                if (File.Exists(filePath))
                {
                    var overwriteResult = MessageBox.Show(
                        $"チェックリスト「{fileName}」は既に存在します。\n上書きしますか？",
                        "確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (overwriteResult != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // ChecklistSaverをサービスプロバイダーから取得
                var checklistSaver = _serviceProvider.GetRequiredService<Infrastructure.Services.ChecklistSaver>();

                // 空のチェックリストを作成
                var emptyCheckItems = new List<CheckItem>();

                // JSON形式で保存
                await checklistSaver.SaveAsync(emptyCheckItems, filePath);

                _logger.LogInformation("新規チェックリストファイルを作成しました: {FilePath}", filePath);

                // 設定を更新して新しいチェックリストを使用
                _pathSettings.SelectedChecklistFile = fileName;

                // データベースの既存チェック項目をクリア（新しいチェックリスト用）
                var existingItems = await _checkItemRepository.GetAllWithChildrenAsync();
                foreach (var item in existingItems)
                {
                    await _checkItemRepository.DeleteAsync(item.Id);
                }

                _logger.LogInformation("既存のチェック項目をクリアしました");

                // UIを再読み込み（空の状態）
                CheckItemsContainer.Children.Clear();
                CheckItemCountText.Text = "0 件";

                StatusText.Text = $"新規チェックリスト「{checklistName}」を作成しました";

                MessageBox.Show(
                    $"新規チェックリスト「{checklistName}」を作成しました。\n\nファイル: {fileName}\n\nアプリケーションを再起動してください。",
                    "作成完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新規チェックリストの作成に失敗しました");
            MessageBox.Show(
                $"新規チェックリストの作成に失敗しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("アプリケーションを終了します");
        Close();
    }

    private void IntegrityCheckMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("データ整合性チェックウィンドウを開きます");

            var integrityLogger = _serviceProvider.GetRequiredService<ILogger<IntegrityReportWindow>>();
            var integrityWindow = new IntegrityReportWindow(_dataIntegrityService, integrityLogger)
            {
                Owner = this
            };

            integrityWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "データ整合性チェックウィンドウの表示に失敗しました");
            MessageBox.Show($"データ整合性チェックウィンドウの表示に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ドキュメントルートディレクトリを取得
    /// </summary>
    private string GetProjectRoot()
    {
        return _documentRootPath;
    }

    /// <summary>
    /// 絶対パスから相対パスを取得
    /// </summary>
    private string GetRelativePath(string absolutePath)
    {
        var projectRoot = GetProjectRoot();
        return Path.GetRelativePath(projectRoot, absolutePath);
    }

    /// <summary>
    /// 資料追加ボタンクリック
    /// </summary>
    private async void AddDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("資料追加ダイアログを開きます");

            var dialog = new OpenFileDialog
            {
                Title = "資料ファイルを選択",
                Filter = "すべてのファイル (*.*)|*.*|PDFファイル (*.pdf)|*.pdf|Wordファイル (*.docx;*.doc)|*.docx;*.doc|Excelファイル (*.xlsx;*.xls)|*.xlsx;*.xls",
                FilterIndex = 1,
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                StatusText.Text = "資料を登録中...";
                var successCount = 0;
                var skipCount = 0;

                foreach (var filePath in dialog.FileNames)
                {
                    var result = await RegisterDocumentAsync(filePath);
                    if (result) successCount++;
                    else skipCount++;
                }

                // リストを再読み込み
                await RefreshDocumentListAsync();

                var message = $"{successCount} 件の資料を登録しました";
                if (skipCount > 0) message += $"（{skipCount} 件はスキップ）";
                StatusText.Text = message;

                _logger.LogInformation(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料の追加に失敗しました");
            StatusText.Text = $"エラー: {ex.Message}";
            MessageBox.Show($"資料の追加に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ファイルを登録する
    /// </summary>
    private async Task<bool> RegisterDocumentAsync(string filePath)
    {
        try
        {
            // ファイル存在チェック
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("ファイルが見つかりません: {FilePath}", filePath);
                MessageBox.Show($"ファイルが見つかりません:\n{filePath}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var projectRoot = GetProjectRoot();

            // documentRootPath 配下に直接ファイルを配置
            // ファイル名と拡張子を取得
            var fileName = Path.GetFileName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            // コピー先のパスを決定（重複があれば連番を追加）
            var destFileName = fileName;
            var destPath = Path.Combine(projectRoot, destFileName);
            var counter = 1;

            while (File.Exists(destPath))
            {
                destFileName = $"{fileNameWithoutExt}_{counter}{extension}";
                destPath = Path.Combine(projectRoot, destFileName);
                counter++;
            }

            // ファイル名のみを相対パスとして保存（プロジェクト内のファイルパスを確実にするため）
            var relativePath = destFileName;

            // 重複チェック（ファイル名で）
            var existing = await _documentRepository.GetByRelativePathAsync(relativePath);
            if (existing != null)
            {
                _logger.LogInformation("既に登録済みの資料です: {RelativePath}", relativePath);
                MessageBox.Show($"この資料は既に登録されています:\n{relativePath}", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // documentRootPathにファイルをコピー（元のファイルと異なる場合のみ）
            var sourceFullPath = Path.GetFullPath(filePath);
            var destFullPath = Path.GetFullPath(destPath);

            if (!string.Equals(sourceFullPath, destFullPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(filePath, destPath, overwrite: false);
                _logger.LogInformation("ファイルをコピーしました: {Source} -> {Dest}", filePath, destPath);
            }
            else
            {
                _logger.LogInformation("ファイルは既にdocumentRoot内にあります: {Path}", filePath);
            }

            // Document エンティティ作成
            var document = new Document
            {
                FileName = destFileName,
                RelativePath = relativePath,
                FileType = extension,
                AddedAt = DateTime.UtcNow
            };

            // データベースに追加
            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            _logger.LogInformation("資料を登録しました: {FileName} ({RelativePath})", document.FileName, document.RelativePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料の登録に失敗しました: {FilePath}", filePath);
            MessageBox.Show($"資料の登録に失敗しました:\n{Path.GetFileName(filePath)}\n\nエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// 資料リストを再読み込み
    /// </summary>
    private async Task RefreshDocumentListAsync()
    {
        var documents = await _documentRepository.GetAllAsync();
        DocumentsListView.ItemsSource = documents;
        DocumentCountText.Text = $"{documents.Count} 件";
    }

    /// <summary>
    /// ドラッグエンター（ファイルのドラッグ&ドロップ受け入れ）
    /// </summary>
    private void DocumentsListView_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    /// <summary>
    /// ドロップ（ファイルを登録）
    /// </summary>
    private async void DocumentsListView_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                _logger.LogInformation("{Count} 個のファイルがドロップされました", files.Length);

                StatusText.Text = "資料を登録中...";
                var successCount = 0;
                var skipCount = 0;

                foreach (var filePath in files)
                {
                    var result = await RegisterDocumentAsync(filePath);
                    if (result) successCount++;
                    else skipCount++;
                }

                // リストを再読み込み
                await RefreshDocumentListAsync();

                var message = $"{successCount} 件の資料を登録しました";
                if (skipCount > 0) message += $"（{skipCount} 件はスキップ）";
                StatusText.Text = message;

                _logger.LogInformation(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ドラッグ&ドロップでの資料登録に失敗しました");
            StatusText.Text = $"エラー: {ex.Message}";
            MessageBox.Show($"資料の登録に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 資料をダブルクリックで開く（ファイルとチェックリストウィンドウを同時に開く）
    /// </summary>
    private void DocumentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (DocumentsListView.SelectedItem is Document document)
            {
                _logger.LogInformation("資料を開きます: {FileName}", document.FileName);

                var projectRoot = GetProjectRoot();
                _logger.LogInformation("プロジェクトルート: {ProjectRoot}", projectRoot);
                _logger.LogInformation("RelativePath (DB保存値): {RelativePath}", document.RelativePath);

                // パス構築: documentRootPath + RelativePath（通常はファイル名のみ）
                var absolutePath = Path.Combine(projectRoot, document.RelativePath);
                _logger.LogInformation("絶対パス構築: {AbsolutePath}", absolutePath);

                // ファイル存在チェック
                if (!File.Exists(absolutePath))
                {
                    _logger.LogWarning("ファイルが見つかりません: {AbsolutePath}", absolutePath);
                    MessageBox.Show(
                        $"ファイルが見つかりません:\n{document.RelativePath}\n\n絶対パス: {absolutePath}\n\nプロジェクトルート: {projectRoot}",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Viewerプロジェクトのパスを取得
                var viewerPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "DocumentFileManager.Viewer", "bin", "Debug", "net9.0-windows",
                    "DocumentFileManager.Viewer.exe");
                viewerPath = Path.GetFullPath(viewerPath);

                var extension = Path.GetExtension(absolutePath).ToLower();

                // ViewerWindowを同じプロセス内で開く
                _logger.LogInformation("ViewerWindowで開きます: {FilePath}", absolutePath);
                var viewerWindow = new DocumentFileManager.Viewer.ViewerWindow(absolutePath);

                // ファイルオープン完了イベントを購読
                viewerWindow.FileOpened += (sender, windowHandle) =>
                {
                    _lastOpenedDocumentWindowHandle = windowHandle;
                    _logger.LogInformation("資料ウィンドウハンドルを取得: {Handle}", windowHandle);

                    // サポート対象ファイルの場合のみChecklistWindowを開く
                    if (IsSupportedByViewer(extension))
                    {
                        OpenChecklistWindow(document, windowHandle);
                    }
                };

                viewerWindow.Show();
                StatusText.Text = $"Viewerで開きました: {document.FileName}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料を開く際にエラーが発生しました");
            MessageBox.Show($"資料を開く際にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Viewerで処理可能なファイル形式かどうかを判定
    /// （Viewerで表示 または Windows標準プログラムで開く）
    /// </summary>
    private bool IsSupportedByViewer(string extension)
    {
        var supportedExtensions = new[]
        {
            // Viewerで表示
            ".png", ".jpg", ".jpeg", ".gif",  // 画像
            ".txt", ".log", ".csv", ".md",    // テキスト
            // Windows標準プログラムで開く（ChecklistWindow連携）
            ".pdf",                           // PDF
            ".msg", ".eml",                   // Email
            ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt",  // Office
            ".3dm", ".sldprt", ".sldasm", ".dwg"  // CAD
        };
        return supportedExtensions.Contains(extension);
    }

    /// <summary>
    /// チェックリストウィンドウを開く
    /// </summary>
    private void OpenChecklistWindow(Document document, IntPtr documentWindowHandle = default)
    {
        try
        {
            // 既存のチェックリストウィンドウを閉じる
            if (_checklistWindow != null)
            {
                _checklistWindow.Close();
                _checklistWindow = null;
            }

            // 新規ウィンドウ作成
            _logger.LogInformation("チェックリストウィンドウを作成します (Document: {FileName}, WindowHandle: {Handle})",
                document.FileName, documentWindowHandle);

            var checkItemUIBuilder = _serviceProvider.GetRequiredService<CheckItemUIBuilder>();
            var checkItemDocumentRepository = _serviceProvider.GetRequiredService<ICheckItemDocumentRepository>();
            var checkItemRepository = _serviceProvider.GetRequiredService<ICheckItemRepository>();
            var checklistSaver = _serviceProvider.GetRequiredService<Infrastructure.Services.ChecklistSaver>();
            var pathSettings = _serviceProvider.GetRequiredService<PathSettings>();
            var checklistLogger = _serviceProvider.GetRequiredService<ILogger<ChecklistWindow>>();
            _checklistWindow = new ChecklistWindow(document, checkItemUIBuilder, checkItemDocumentRepository, checkItemRepository, checklistSaver, pathSettings, checklistLogger, documentWindowHandle)
            {
                Owner = null // Ownerを設定しない（MainWindowとの親子関係を切る）
            };

            // ウィンドウが閉じられたときの処理
            _checklistWindow.Closed += async (s, args) =>
            {
                _checklistWindow = null;

                // MainWindowを再表示
                Show();
                Activate();

                // チェック項目UIを再読み込み（ChecklistWindowでの変更を反映）
                await LoadCheckItemsAsync();

                _logger.LogInformation("チェックリストウィンドウが閉じられました。MainWindowを再表示し、チェック項目を再読み込みしました。");
            };

            // MainWindowを非表示にしてからChecklistWindowを表示
            Hide();
            _checklistWindow.Show();
            _logger.LogInformation("チェックリストウィンドウを表示しました。MainWindowを非表示にしました。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックリストウィンドウの表示に失敗しました");
            Show(); // エラー時はMainWindowを表示
            MessageBox.Show($"チェックリストウィンドウの表示に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region フィルタリングとハイライト機能

    /// <summary>
    /// チェックボックスのイベントを再帰的に登録
    /// </summary>
    private void RegisterCheckBoxEvents(Panel panel)
    {
        foreach (var child in panel.Children)
        {
            if (child is StackPanel stackPanel && stackPanel.Tag != null)
            {
                var tag = stackPanel.Tag;
                var tagType = tag.GetType();
                var checkBoxProperty = tagType.GetProperty("CheckBox");
                var viewModelProperty = tagType.GetProperty("ViewModel");

                if (checkBoxProperty != null && viewModelProperty != null)
                {
                    var checkBox = checkBoxProperty.GetValue(tag) as CheckBox;
                    var viewModel = viewModelProperty.GetValue(tag) as CheckItemViewModel;

                    if (checkBox != null && viewModel != null)
                    {
                        // UIElementsマップに追加
                        _checkItemUIElements[viewModel.Entity.Id] = stackPanel;

                        // クリックイベントを登録
                        checkBox.Click += CheckBox_Click;
                    }
                }
            }
            else if (child is GroupBox groupBox && groupBox.Content is Panel childPanel)
            {
                RegisterCheckBoxEvents(childPanel);
            }
            else if (child is Panel subPanel)
            {
                RegisterCheckBoxEvents(subPanel);
            }
        }
    }

    /// <summary>
    /// チェックボックスクリック時の処理（メインフォーム専用）
    /// </summary>
    private async void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is CheckItemViewModel viewModel)
        {
            // チェック状態の変更をキャンセル（トグルを元に戻す）
            checkBox.IsChecked = !checkBox.IsChecked;

            // 現在選択中のチェック項目と同じ場合は選択解除
            if (_selectedCheckItem?.Entity.Id == viewModel.Entity.Id)
            {
                ClearCheckItemSelection();
                DocumentsListView.ItemsSource = _allDocuments;
                DocumentCountText.Text = $"{_allDocuments.Count} 件";
                _logger.LogInformation("チェック項目の選択を解除しました");
            }
            else
            {
                // 新しいチェック項目を選択してフィルタリング
                await FilterDocumentsByCheckItem(viewModel);
            }
        }
    }

    /// <summary>
    /// すべて表示ボタンクリック
    /// </summary>
    private void ShowAllDocumentsButton_Click(object sender, RoutedEventArgs e)
    {
        ClearCheckItemSelection();
        DocumentsListView.ItemsSource = _allDocuments;
        DocumentCountText.Text = $"{_allDocuments.Count} 件";
        _logger.LogInformation("すべての資料を表示しました");
    }

    /// <summary>
    /// チェック項目コンテナの空白クリック
    /// </summary>
    private void CheckItemsContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // クリック対象がWrapPanel自体（空白部分）の場合のみ処理
        if (e.OriginalSource == CheckItemsContainer)
        {
            ClearCheckItemSelection();
            DocumentsListView.ItemsSource = _allDocuments;
            DocumentCountText.Text = $"{_allDocuments.Count} 件";
            _logger.LogInformation("空白クリックにより、すべての資料を表示しました");
        }
    }

    /// <summary>
    /// 資料選択時のハイライト
    /// </summary>
    private async void DocumentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DocumentsListView.SelectedItem is Document selectedDocument)
        {
            await HighlightCheckItemsByDocument(selectedDocument);
        }
        else
        {
            ClearCheckItemHighlights();
        }
    }

    /// <summary>
    /// チェック項目で資料をフィルタリング
    /// </summary>
    private async Task FilterDocumentsByCheckItem(CheckItemViewModel checkItem)
    {
        try
        {
            // 前回の選択をクリア
            ClearCheckItemSelection();

            // 新しい選択を設定
            _selectedCheckItem = checkItem;

            // チェック項目に紐づく資料を取得
            var checkItemDocumentRepo = _serviceProvider.GetRequiredService<ICheckItemDocumentRepository>();
            var linkedItems = await checkItemDocumentRepo.GetByCheckItemIdAsync(checkItem.Entity.Id);
            var documentIds = linkedItems.Select(x => x.DocumentId).ToHashSet();

            // フィルタリング
            var filteredDocuments = _allDocuments.Where(d => documentIds.Contains(d.Id)).ToList();

            DocumentsListView.ItemsSource = filteredDocuments;
            DocumentCountText.Text = $"{filteredDocuments.Count} 件（フィルタリング中）";

            // 選択されたチェック項目をハイライト（薄い青）
            if (_checkItemUIElements.TryGetValue(checkItem.Entity.Id, out var stackPanel))
            {
                stackPanel.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // #E3F2FD
            }

            _logger.LogInformation("チェック項目 '{Label}' に紐づく資料 {Count} 件を表示しました", checkItem.Label, filteredDocuments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "フィルタリング処理に失敗しました");
            MessageBox.Show($"フィルタリング処理に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// チェック項目の選択をクリア
    /// </summary>
    private void ClearCheckItemSelection()
    {
        if (_selectedCheckItem != null && _checkItemUIElements.TryGetValue(_selectedCheckItem.Entity.Id, out var stackPanel))
        {
            stackPanel.Background = Brushes.Transparent;
        }
        _selectedCheckItem = null;
    }

    /// <summary>
    /// 資料に紐づくチェック項目をハイライト
    /// </summary>
    private async Task HighlightCheckItemsByDocument(Document document)
    {
        try
        {
            // 前回のハイライトをクリア
            ClearCheckItemHighlights();

            // 資料に紐づくチェック項目を取得
            var checkItemDocumentRepo = _serviceProvider.GetRequiredService<ICheckItemDocumentRepository>();
            var linkedItems = await checkItemDocumentRepo.GetByDocumentIdAsync(document.Id);
            var checkItemIds = linkedItems.Select(x => x.CheckItemId).ToHashSet();

            // ハイライト（薄い黄色）
            foreach (var checkItemId in checkItemIds)
            {
                if (_checkItemUIElements.TryGetValue(checkItemId, out var stackPanel))
                {
                    stackPanel.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196)); // #FFF9C4
                }
            }

            _logger.LogInformation("資料 '{FileName}' に紐づく {Count} 件のチェック項目をハイライトしました", document.FileName, checkItemIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ハイライト処理に失敗しました");
        }
    }

    /// <summary>
    /// チェック項目のハイライトをクリア
    /// </summary>
    private void ClearCheckItemHighlights()
    {
        foreach (var stackPanel in _checkItemUIElements.Values)
        {
            // 選択中のチェック項目（フィルタリング用）は青いまま維持
            if (_selectedCheckItem != null && stackPanel == _checkItemUIElements[_selectedCheckItem.Entity.Id])
            {
                continue;
            }
            stackPanel.Background = Brushes.Transparent;
        }
    }

    #endregion
}