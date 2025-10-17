using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocumentFileManager.UI;

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

    // チェックリストウィンドウ（シングルトン管理）
    private ChecklistWindow? _checklistWindow;
    private IntPtr _lastOpenedDocumentWindowHandle = IntPtr.Zero;

    public MainWindow(
        IDocumentRepository documentRepository,
        ICheckItemRepository checkItemRepository,
        CheckItemUIBuilder checkItemUIBuilder,
        UISettings uiSettings,
        PathSettings pathSettings,
        IServiceProvider serviceProvider,
        IDataIntegrityService dataIntegrityService,
        ILogger<MainWindow> logger)
    {
        _documentRepository = documentRepository;
        _checkItemRepository = checkItemRepository;
        _checkItemUIBuilder = checkItemUIBuilder;
        _uiSettings = uiSettings;
        _pathSettings = pathSettings;
        _serviceProvider = serviceProvider;
        _dataIntegrityService = dataIntegrityService;
        _logger = logger;

        InitializeComponent();

        // ウィンドウが閉じられたときにアプリケーションを確実にシャットダウン
        Closed += (s, e) => Application.Current.Shutdown();

        _logger.LogInformation("MainWindow が初期化されました");
    }

    private async void LoadDocumentsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("資料の読み込みを開始します");
            StatusText.Text = "資料を読み込み中...";

            var documents = await _documentRepository.GetAllAsync();
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

    private async void LoadCheckItemsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("チェック項目の読み込みを開始します");
            StatusText.Text = "チェック項目を読み込み中...";

            // UIBuilderを使用してGroupBox階層を構築
            await _checkItemUIBuilder.BuildAsync(CheckItemsContainer);

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
            var settingsWindow = new SettingsWindow(_uiSettings, pathSettings, settingsLogger)
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
    /// プロジェクトルートディレクトリを取得
    /// </summary>
    private string GetProjectRoot()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var pathSegments = Enumerable.Repeat("..", _pathSettings.ProjectRootLevelsUp).ToArray();
        return Path.GetFullPath(Path.Combine(new[] { baseDirectory }.Concat(pathSegments).ToArray()));
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
            var documentsDir = Path.Combine(projectRoot, _pathSettings.DocumentsDirectory);

            // DocumentsDirectoryが存在しない場合は作成
            if (!Directory.Exists(documentsDir))
            {
                Directory.CreateDirectory(documentsDir);
                _logger.LogInformation("資料保存ディレクトリを作成しました: {Path}", documentsDir);
            }

            // ファイル名と拡張子を取得
            var fileName = Path.GetFileName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            // コピー先のパスを決定（重複があれば連番を追加）
            var destFileName = fileName;
            var destPath = Path.Combine(documentsDir, destFileName);
            var counter = 1;

            while (File.Exists(destPath))
            {
                destFileName = $"{fileNameWithoutExt}_{counter}{extension}";
                destPath = Path.Combine(documentsDir, destFileName);
                counter++;
            }

            // DocumentsDirectoryを基準とした相対パス
            var relativePath = Path.Combine(_pathSettings.DocumentsDirectory, destFileName);

            // 重複チェック（相対パスで）
            var existing = await _documentRepository.GetByRelativePathAsync(relativePath);
            if (existing != null)
            {
                _logger.LogInformation("既に登録済みの資料です: {RelativePath}", relativePath);
                MessageBox.Show($"この資料は既に登録されています:\n{relativePath}", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // DocumentsDirectoryにファイルをコピー（元のファイルと異なる場合のみ）
            var sourceFullPath = Path.GetFullPath(filePath);
            var destFullPath = Path.GetFullPath(destPath);

            if (!string.Equals(sourceFullPath, destFullPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(filePath, destPath, overwrite: false);
                _logger.LogInformation("ファイルをコピーしました: {Source} -> {Dest}", filePath, destPath);
            }
            else
            {
                _logger.LogInformation("ファイルは既にDocumentsDirectory内にあります: {Path}", filePath);
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
                _logger.LogInformation("相対パス: {RelativePath}", document.RelativePath);
                var absolutePath = Path.Combine(projectRoot, document.RelativePath);
                _logger.LogInformation("絶対パス: {AbsolutePath}", absolutePath);

                // ファイル存在チェック
                if (!File.Exists(absolutePath))
                {
                    _logger.LogWarning("ファイルが見つかりません: {AbsolutePath}", absolutePath);
                    MessageBox.Show(
                        $"ファイルが見つかりません:\n{document.RelativePath}\n\n絶対パス: {absolutePath}",
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
            var pathSettings = _serviceProvider.GetRequiredService<PathSettings>();
            var checklistLogger = _serviceProvider.GetRequiredService<ILogger<ChecklistWindow>>();
            _checklistWindow = new ChecklistWindow(document, checkItemUIBuilder, checkItemDocumentRepository, pathSettings, checklistLogger, documentWindowHandle)
            {
                Owner = null // Ownerを設定しない（MainWindowとの親子関係を切る）
            };

            // ウィンドウが閉じられたときの処理
            _checklistWindow.Closed += (s, args) =>
            {
                _checklistWindow = null;
                // MainWindowを再表示
                Show();
                Activate();
                _logger.LogInformation("チェックリストウィンドウが閉じられました。MainWindowを再表示しました。");
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
}