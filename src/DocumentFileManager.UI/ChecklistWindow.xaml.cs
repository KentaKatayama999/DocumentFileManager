using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI;

/// <summary>
/// チェックリストウィンドウ
/// </summary>
public partial class ChecklistWindow : Window
{
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int SWP_NOMOVE = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    private readonly CheckItemUIBuilder _checkItemUIBuilder;
    private readonly ICheckItemDocumentRepository _checkItemDocumentRepository;
    private readonly PathSettings _pathSettings;
    private readonly ILogger<ChecklistWindow> _logger;
    private readonly Document _document;
    private readonly ScreenCaptureService _captureService;
    private bool _isDockingRight = true; // デフォルトは右端
    private bool _isAdjustingPosition = false; // 位置調整中フラグ
    private IntPtr _documentWindowHandle = IntPtr.Zero; // 開いた資料のウィンドウハンドル

    public ChecklistWindow(
        Document document,
        CheckItemUIBuilder checkItemUIBuilder,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        PathSettings pathSettings,
        ILogger<ChecklistWindow> logger)
        : this(document, checkItemUIBuilder, checkItemDocumentRepository, pathSettings, logger, IntPtr.Zero)
    {
    }

    public ChecklistWindow(
        Document document,
        CheckItemUIBuilder checkItemUIBuilder,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        PathSettings pathSettings,
        ILogger<ChecklistWindow> logger,
        IntPtr documentWindowHandle)
    {
        _document = document;
        _checkItemUIBuilder = checkItemUIBuilder;
        _checkItemDocumentRepository = checkItemDocumentRepository;
        _pathSettings = pathSettings;
        _logger = logger;
        _documentWindowHandle = documentWindowHandle;
        _captureService = new ScreenCaptureService();

        InitializeComponent();

        // ウィンドウタイトルにドキュメント名を設定
        Title = $"チェックリスト - {_document.FileName}";

        // 常に手前に表示をデフォルトでON
        Topmost = true;

        // ウィンドウサイズを画面の1/3幅×全高に設定、右端に配置
        InitializeWindowSize();

        // ウィンドウが読み込まれたときにチェック項目を読み込み
        Loaded += ChecklistWindow_Loaded;
        SourceInitialized += ChecklistWindow_SourceInitialized;

        // 位置と高さを固定するためのイベントハンドラ
        SizeChanged += ChecklistWindow_SizeChanged;

        _logger.LogInformation("ChecklistWindow が初期化されました (Document: {FileName})", _document.FileName);
    }

    /// <summary>
    /// ウィンドウハンドルが初期化されたときにWin32フックを設定
    /// </summary>
    private void ChecklistWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// ウィンドウプロシージャ（移動をブロック）
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_WINDOWPOSCHANGING && !_isAdjustingPosition)
        {
            var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);

            // 移動をブロック（SWP_NOMOVEフラグを追加）
            windowPos.flags |= SWP_NOMOVE;

            Marshal.StructureToPtr(windowPos, lParam, true);
            handled = false; // 他の処理も継続させる
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// ウィンドウサイズを初期化（画面の1/3幅×全高、右端に配置）
    /// </summary>
    private void InitializeWindowSize()
    {
        var workArea = SystemParameters.WorkArea;
        Width = workArea.Width / 3.0;
        Height = workArea.Height;

        // デフォルトで右端に配置
        Left = workArea.Right - Width;
        Top = workArea.Top;

        _logger.LogDebug("ウィンドウサイズを初期化: {Width}x{Height}、位置: ({Left}, {Top})", Width, Height, Left, Top);
    }

    /// <summary>
    /// ウィンドウが読み込まれたときの処理
    /// </summary>
    private async void ChecklistWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("チェック項目の読み込みを開始します (Document: {DocumentId})", _document.Id);

            // UIBuilderを使用してGroupBox階層を構築（Documentと紐づけて）
            // キャプチャ要求デリゲートを渡す
            await _checkItemUIBuilder.BuildAsync(CheckItemsContainer, _document, PerformCaptureForCheckItem);

            _logger.LogInformation("チェック項目の階層表示が完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック項目の読み込みに失敗しました");
            MessageBox.Show($"チェック項目の読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 常に手前に表示のチェック状態が変更されたとき
    /// </summary>
    private void TopmostCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        Topmost = true;
        _logger.LogDebug("常に手前に表示: ON");
    }

    /// <summary>
    /// 常に手前に表示のチェックが外されたとき
    /// </summary>
    private void TopmostCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        Topmost = false;
        _logger.LogDebug("常に手前に表示: OFF");
    }

    /// <summary>
    /// サイズが変更されたときに高さを画面いっぱいに固定
    /// </summary>
    private void ChecklistWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isAdjustingPosition) return;

        _isAdjustingPosition = true;
        try
        {
            var workArea = SystemParameters.WorkArea;

            // 高さを画面いっぱいに固定
            if (Math.Abs(ActualHeight - workArea.Height) > 1)
            {
                Height = workArea.Height;
            }

            // 位置も調整（幅が変更されたときに位置がずれないように）
            if (_isDockingRight)
            {
                Left = workArea.Right - ActualWidth;
            }
            else
            {
                Left = workArea.Left;
            }

            Top = workArea.Top;
        }
        finally
        {
            _isAdjustingPosition = false;
        }
    }

    /// <summary>
    /// 左に配置ボタンクリック
    /// </summary>
    private void DockLeftButton_Click(object sender, RoutedEventArgs e)
    {
        _isDockingRight = false;
        _isAdjustingPosition = true;

        try
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left;
            Top = workArea.Top;
            Height = workArea.Height;

            _logger.LogDebug("ウィンドウを左端に配置: Left={Left}, Top={Top}", Left, Top);
        }
        finally
        {
            _isAdjustingPosition = false;
        }
    }

    /// <summary>
    /// 右に配置ボタンクリック
    /// </summary>
    private void DockRightButton_Click(object sender, RoutedEventArgs e)
    {
        _isDockingRight = true;
        _isAdjustingPosition = true;

        try
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth;
            Top = workArea.Top;
            Height = workArea.Height;

            _logger.LogDebug("ウィンドウを右端に配置: Left={Left}, Top={Top}", Left, Top);
        }
        finally
        {
            _isAdjustingPosition = false;
        }
    }

    /// <summary>
    /// キャプチャボタンクリック
    /// </summary>
    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        PerformCapture();
    }

    /// <summary>
    /// キャプチャ処理を実行
    /// </summary>
    private void PerformCapture()
    {
        try
        {
            _logger.LogInformation("画面キャプチャを開始します（範囲選択モード）");

            // 資料ウィンドウの範囲を取得して初期選択範囲として設定
            ScreenCaptureOverlay overlay;
            if (_documentWindowHandle != IntPtr.Zero && GetWindowRect(_documentWindowHandle, out RECT rect))
            {
                // ウィンドウの矩形領域を取得
                var windowArea = new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                _logger.LogInformation("資料ウィンドウの範囲を初期選択: X={X}, Y={Y}, Width={Width}, Height={Height}",
                    windowArea.X, windowArea.Y, windowArea.Width, windowArea.Height);
                overlay = new ScreenCaptureOverlay(windowArea);
            }
            else
            {
                // ウィンドウハンドルがない場合は通常の範囲選択
                _logger.LogInformation("資料ウィンドウハンドルがないため、手動範囲選択モードで開始");
                overlay = new ScreenCaptureOverlay();
            }

            bool? result = overlay.ShowDialog();

            if (result == true && overlay.SelectedArea.HasValue)
            {
                var selectedArea = overlay.SelectedArea.Value;
                _logger.LogInformation("選択範囲: X={X}, Y={Y}, Width={Width}, Height={Height}",
                    selectedArea.X, selectedArea.Y, selectedArea.Width, selectedArea.Height);

                // 選択範囲をキャプチャ
                var rectangle = new System.Drawing.Rectangle(
                    (int)selectedArea.X,
                    (int)selectedArea.Y,
                    (int)selectedArea.Width,
                    (int)selectedArea.Height);

                using (var bitmap = CaptureRectangle(rectangle))
                {
                    // BitmapをBitmapSourceに変換
                    var bitmapSource = ConvertBitmapToBitmapSource(bitmap);

                    // プレビューウィンドウを表示
                    var previewWindow = new ImagePreviewWindow(bitmapSource);
                    bool? previewResult = previewWindow.ShowDialog();

                    if (previewWindow.RecaptureRequested)
                    {
                        // 再キャプチャが要求された場合
                        _logger.LogInformation("再キャプチャが要求されました");
                        PerformCapture(); // 再帰呼び出し
                    }
                }

                _logger.LogInformation("キャプチャ処理が完了しました");
            }
            else
            {
                _logger.LogInformation("キャプチャがキャンセルされました");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "キャプチャ処理中にエラーが発生しました");
            MessageBox.Show(
                $"キャプチャ処理中にエラーが発生しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 指定された矩形領域をキャプチャ
    /// </summary>
    private System.Drawing.Bitmap CaptureRectangle(System.Drawing.Rectangle bounds)
    {
        var bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, System.Drawing.CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    /// <summary>
    /// System.Drawing.BitmapをWPF BitmapSourceに変換
    /// </summary>
    private BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    /// <summary>
    /// チェック項目に対してキャプチャ処理を実行
    /// </summary>
    private async Task PerformCaptureForCheckItem(CheckItemViewModel viewModel, UIElement checkBoxContainer)
    {
        try
        {
            _logger.LogInformation("チェック項目のキャプチャを開始: {Path}", viewModel.Path);

            // プロジェクトルートを取得
            var projectRoot = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "..", "..", "..", "..", "..");
            projectRoot = Path.GetFullPath(projectRoot);

            // キャプチャファイルパスを生成
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var capturesDir = Path.Combine(projectRoot, _pathSettings.CapturesDirectory, $"document_{_document.Id}");
            var fileName = $"checkitem_{viewModel.Entity.Id}_{timestamp}.png";
            var relativePath = Path.Combine(_pathSettings.CapturesDirectory, $"document_{_document.Id}", fileName);
            var absolutePath = Path.Combine(projectRoot, relativePath);

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(capturesDir))
            {
                Directory.CreateDirectory(capturesDir);
                _logger.LogInformation("キャプチャディレクトリを作成: {Path}", capturesDir);
            }

            // 範囲選択オーバーレイを表示
            ScreenCaptureOverlay overlay;
            if (_documentWindowHandle != IntPtr.Zero && GetWindowRect(_documentWindowHandle, out RECT rect))
            {
                var windowArea = new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                overlay = new ScreenCaptureOverlay(windowArea);
            }
            else
            {
                overlay = new ScreenCaptureOverlay();
            }

            bool? overlayResult = overlay.ShowDialog();

            if (overlayResult == true && overlay.SelectedArea.HasValue)
            {
                var selectedArea = overlay.SelectedArea.Value;

                // 選択範囲をキャプチャ
                var rectangle = new System.Drawing.Rectangle(
                    (int)selectedArea.X,
                    (int)selectedArea.Y,
                    (int)selectedArea.Width,
                    (int)selectedArea.Height);

                using (var bitmap = CaptureRectangle(rectangle))
                {
                    // BitmapをBitmapSourceに変換
                    var bitmapSource = ConvertBitmapToBitmapSource(bitmap);

                    // プレビューウィンドウを自動保存モードで表示
                    var previewWindow = new ImagePreviewWindow(bitmapSource, absolutePath);
                    bool? previewResult = previewWindow.ShowDialog();

                    if (previewWindow.RecaptureRequested)
                    {
                        // 再キャプチャが要求された場合
                        _logger.LogInformation("再キャプチャが要求されました");
                        await PerformCaptureForCheckItem(viewModel, checkBoxContainer);
                        return;
                    }

                    // 保存が成功した場合
                    if (previewResult == true && !string.IsNullOrEmpty(previewWindow.SavedFilePath))
                    {
                        _logger.LogInformation("キャプチャ画像を保存: {Path}", relativePath);

                        // ViewModelを更新
                        viewModel.CaptureFilePath = relativePath;

                        // DBを更新
                        var linkedItem = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                            _document.Id, viewModel.Entity.Id);

                        if (linkedItem != null)
                        {
                            await _checkItemDocumentRepository.UpdateCaptureFileAsync(linkedItem.Id, relativePath);
                            await _checkItemDocumentRepository.SaveChangesAsync();
                            _logger.LogInformation("DB更新完了: CheckItemDocument.Id={Id}, CaptureFile={Path}",
                                linkedItem.Id, relativePath);
                        }

                        // UIを更新（🖼️ボタンを表示）
                        if (checkBoxContainer is StackPanel stackPanel)
                        {
                            // StackPanelの2番目の子要素がButton（🖼️）
                            if (stackPanel.Children.Count >= 2 && stackPanel.Children[1] is Button imageButton)
                            {
                                imageButton.Visibility = Visibility.Visible;
                                _logger.LogInformation("画像確認ボタンを表示");
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("キャプチャがキャンセルされました");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック項目のキャプチャ処理中にエラーが発生しました");
            MessageBox.Show(
                $"キャプチャ処理中にエラーが発生しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
