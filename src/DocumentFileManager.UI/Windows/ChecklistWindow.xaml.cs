using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Dialogs;
using DocumentFileManager.UI.Helpers;
using DocumentFileManager.UI.Services;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.Logging;
using WinForms = System.Windows.Forms;

namespace DocumentFileManager.UI.Windows;

/// <summary>
/// チェックリストウィンドウ
/// </summary>
public partial class ChecklistWindow : Window
{
    #region Win32 API Constants

    // Window Messages
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_SYSCOMMAND = 0x0112;

    // SetWindowPos Flags
    private const int SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    // ShowWindow Commands
    private const int SW_RESTORE = 9;
    private const int SW_SHOWNORMAL = 1;

    // System Commands
    private const int SC_RESTORE = 0xF120;

    // Delay Constants (milliseconds)
    /// <summary>
    /// ウィンドウ状態復元後の待機時間（ミリ秒）
    /// </summary>
    private const int WINDOW_STATE_RESTORE_DELAY_MS = 100;

    /// <summary>
    /// DPIコンテキスト更新待機時間（ミリ秒）
    /// </summary>
    private const int DPI_CONTEXT_UPDATE_DELAY_MS = 150;

    #endregion

    #region Win32 API Structures

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

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public System.Drawing.Point ptMinPosition;
        public System.Drawing.Point ptMaxPosition;
        public System.Drawing.Rectangle rcNormalPosition;
    }

    #endregion

    #region Win32 API Methods

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    #endregion

    #region Fields

    private readonly CheckItemUIBuilder _checkItemUIBuilder;
    private readonly ICheckItemDocumentRepository _checkItemDocumentRepository;
    private readonly ICheckItemRepository _checkItemRepository;
    private readonly Infrastructure.Services.ChecklistSaver _checklistSaver;
    private readonly PathSettings _pathSettings;
    private readonly ILogger<ChecklistWindow> _logger;
    private readonly Document _document;
    private readonly ScreenCaptureService _captureService;
    private readonly string _documentRootPath;

    /// <summary>
    /// チェックリストの配置位置（true=右端、false=左端）
    /// </summary>
    private bool _isDockingRight = true;

    /// <summary>
    /// 位置調整処理中フラグ（WndProcフックとの競合回避用）
    /// </summary>
    private bool _isAdjustingPosition = false;

    /// <summary>
    /// 開いた資料のウィンドウハンドル（Excel等の外部アプリ用）
    /// </summary>
    private IntPtr _documentWindowHandle = IntPtr.Zero;

    /// <summary>
    /// 内部PDFビューアウィンドウへの参照
    /// </summary>
    private DocumentFileManager.Viewer.ViewerWindow? _viewerWindow;

    #endregion

    #region Constructors

    public ChecklistWindow(
        Document document,
        CheckItemUIBuilder checkItemUIBuilder,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        ICheckItemRepository checkItemRepository,
        Infrastructure.Services.ChecklistSaver checklistSaver,
        PathSettings pathSettings,
        ILogger<ChecklistWindow> logger,
        string documentRootPath)
        : this(document, checkItemUIBuilder, checkItemDocumentRepository, checkItemRepository, checklistSaver, pathSettings, logger, documentRootPath, IntPtr.Zero, null)
    {
    }

    public ChecklistWindow(
        Document document,
        CheckItemUIBuilder checkItemUIBuilder,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        ICheckItemRepository checkItemRepository,
        Infrastructure.Services.ChecklistSaver checklistSaver,
        PathSettings pathSettings,
        ILogger<ChecklistWindow> logger,
        string documentRootPath,
        IntPtr documentWindowHandle)
        : this(document, checkItemUIBuilder, checkItemDocumentRepository, checkItemRepository, checklistSaver, pathSettings, logger, documentRootPath, documentWindowHandle, null)
    {
    }

    public ChecklistWindow(
        Document document,
        CheckItemUIBuilder checkItemUIBuilder,
        ICheckItemDocumentRepository checkItemDocumentRepository,
        ICheckItemRepository checkItemRepository,
        Infrastructure.Services.ChecklistSaver checklistSaver,
        PathSettings pathSettings,
        ILogger<ChecklistWindow> logger,
        string documentRootPath,
        IntPtr documentWindowHandle,
        DocumentFileManager.Viewer.ViewerWindow? viewerWindow)
    {
        _document = document;
        _checkItemUIBuilder = checkItemUIBuilder;
        _checkItemDocumentRepository = checkItemDocumentRepository;
        _checkItemRepository = checkItemRepository;
        _checklistSaver = checklistSaver;
        _pathSettings = pathSettings;
        _logger = logger;
        _documentRootPath = documentRootPath;
        _documentWindowHandle = documentWindowHandle;
        _viewerWindow = viewerWindow;
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

        // ウィンドウが閉じられたときに資料ウィンドウも閉じる
        Closed += ChecklistWindow_Closed;

        // ウィンドウがアクティブになったときにチェック項目を再読み込み
        Activated += ChecklistWindow_Activated;

        _logger.LogInformation("ChecklistWindow が初期化されました (Document: {FileName})", _document.FileName);
    }

    private bool _isFirstActivation = true;

    /// <summary>
    /// ウィンドウがアクティブになったときの処理
    /// </summary>
    private async void ChecklistWindow_Activated(object? sender, EventArgs e)
    {
        // 初回アクティベーションはスキップ（ChecklistWindow_Loadedで読み込み済み）
        if (_isFirstActivation)
        {
            _isFirstActivation = false;
            return;
        }

        // チェック項目を再読み込み
        await RefreshCheckItemsAsync();
    }

    #endregion

    #region Window Event Handlers

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
            try
            {
                var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);

                // 移動をブロック（SWP_NOMOVEフラグを追加）
                windowPos.flags |= SWP_NOMOVE;

                Marshal.StructureToPtr(windowPos, lParam, fDeleteOld: true);
                handled = false; // 他の処理も継続させる
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WndProcでエラーが発生しました");
            }
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
    /// ウィンドウが閉じられたときに資料ウィンドウも閉じる
    /// </summary>
    private void ChecklistWindow_Closed(object? sender, EventArgs e)
    {
        try
        {
            if (_documentWindowHandle != IntPtr.Zero)
            {
                _logger.LogInformation("資料ウィンドウを閉じます (Handle: {Handle})", _documentWindowHandle);
                PostMessage(_documentWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "資料ウィンドウを閉じる際にエラーが発生しました");
        }
    }

    #endregion

    #region Toolbar Event Handlers

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
    /// 閉じるボタンクリック
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
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

            // ChecklistWindowを左に配置
            Left = workArea.Left;
            Top = workArea.Top;
            Height = workArea.Height;

            _logger.LogDebug("ChecklistWindowを左端に配置: Left={Left}, Top={Top}", Left, Top);

            // ViewerWindow（資料ウィンドウ）を右に配置
            if (_documentWindowHandle != IntPtr.Zero)
            {
                ShowWindow(_documentWindowHandle, SW_RESTORE); // 最大化を解除
                int viewerX = (int)(workArea.Left + ActualWidth);
                int viewerY = (int)workArea.Top;
                int viewerWidth = (int)(workArea.Width - ActualWidth);
                int viewerHeight = (int)workArea.Height;

                SetWindowPos(_documentWindowHandle, IntPtr.Zero, viewerX, viewerY, viewerWidth, viewerHeight, 0);
                _logger.LogDebug("ViewerWindowを右端に配置: X={X}, Y={Y}, Width={Width}, Height={Height}",
                    viewerX, viewerY, viewerWidth, viewerHeight);
            }
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

            // ChecklistWindowを右に配置
            Left = workArea.Right - ActualWidth;
            Top = workArea.Top;
            Height = workArea.Height;

            _logger.LogDebug("ChecklistWindowを右端に配置: Left={Left}, Top={Top}", Left, Top);

            // ViewerWindow（資料ウィンドウ）を左に配置
            if (_documentWindowHandle != IntPtr.Zero)
            {
                ShowWindow(_documentWindowHandle, SW_RESTORE); // 最大化を解除
                int viewerX = (int)workArea.Left;
                int viewerY = (int)workArea.Top;
                int viewerWidth = (int)(workArea.Width - ActualWidth);
                int viewerHeight = (int)workArea.Height;

                SetWindowPos(_documentWindowHandle, IntPtr.Zero, viewerX, viewerY, viewerWidth, viewerHeight, 0);
                _logger.LogDebug("ViewerWindowを左端に配置: X={X}, Y={Y}, Width={Width}, Height={Height}",
                    viewerX, viewerY, viewerWidth, viewerHeight);
            }
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
    /// 項目追加ボタンクリック
    /// </summary>
    private async void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (success, itemName) = ShowInputDialog(
                "チェック項目の追加",
                "項目名を入力してください:");

            if (success && !string.IsNullOrWhiteSpace(itemName))
            {
                _logger.LogInformation("チェック項目を追加: {ItemName}", itemName);

                // 「追加項目」カテゴリの存在確認
                var additionalItemsCategory = await _checkItemRepository.GetByPathAsync("追加項目");

                if (additionalItemsCategory == null)
                {
                    // 「追加項目」カテゴリを作成
                    additionalItemsCategory = new CheckItem
                    {
                        Path = "追加項目",
                        Label = "追加項目",
                        Status = ValueObjects.ItemStatus.Unspecified,
                        ParentId = null
                    };

                    await _checkItemRepository.AddAsync(additionalItemsCategory);
                    await _checkItemRepository.SaveChangesAsync();

                    _logger.LogInformation("「追加項目」カテゴリを作成しました: Id={Id}", additionalItemsCategory.Id);
                }

                // 新規項目を作成
                var newItem = new CheckItem
                {
                    Path = $"追加項目/{itemName}",
                    Label = itemName,
                    Status = ValueObjects.ItemStatus.Unspecified,
                    ParentId = additionalItemsCategory.Id
                };

                await _checkItemRepository.AddAsync(newItem);
                await _checkItemRepository.SaveChangesAsync();

                _logger.LogInformation("新しいチェック項目を追加しました: Id={Id}, Path={Path}", newItem.Id, newItem.Path);

                // すべてのチェック項目を取得してJSONに保存
                var allCheckItems = await _checkItemRepository.GetAllWithChildrenAsync();

                var jsonFilePath = Path.Combine(_documentRootPath, _pathSettings.SelectedChecklistFile);

                await _checklistSaver.SaveAsync(allCheckItems, jsonFilePath);

                _logger.LogInformation("チェック項目をJSONファイルに保存しました: {FilePath}", jsonFilePath);

                // UIを再読み込み
                CheckItemsContainer.Children.Clear();
                await _checkItemUIBuilder.BuildAsync(CheckItemsContainer, _document, PerformCaptureForCheckItem);

                _logger.LogInformation("チェック項目UIを再読み込みしました");

                MessageBox.Show(
                    $"チェック項目「{itemName}」を追加しました。",
                    "項目追加完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック項目の追加に失敗しました");
            MessageBox.Show(
                $"チェック項目の追加に失敗しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 新規チェックリスト作成ボタンクリック
    /// </summary>
    private async void CreateNewChecklistButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (success, checklistName) = ShowInputDialog(
                "新規チェックリスト作成",
                "新しいチェックリスト名を入力してください:",
                "（例: 建築プロジェクト、設備点検など）");

            if (success && !string.IsNullOrWhiteSpace(checklistName))
            {
                _logger.LogInformation("新規チェックリストを作成: {ChecklistName}", checklistName);

                // ファイル名を生成（checklist_xxx.json形式）
                var safeFileName = string.Concat(checklistName.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"checklist_{safeFileName}.json";

                var projectRoot = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                    "..", "..", "..", "..", "..");
                projectRoot = Path.GetFullPath(projectRoot);

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

                // 空のチェックリストを作成（基本カテゴリのみ）
                var emptyCheckItems = new List<CheckItem>();

                // JSON形式で保存
                await _checklistSaver.SaveAsync(emptyCheckItems, filePath);

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

                MessageBox.Show(
                    $"新規チェックリスト「{checklistName}」を作成しました。\n\nファイル: {fileName}\n\n「項目追加」ボタンから項目を追加してください。",
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

    /// <summary>
    /// チェック項目を再読み込み
    /// </summary>
    private async Task RefreshCheckItemsAsync()
    {
        try
        {
            _logger.LogInformation("チェック項目の再読み込みを開始します (Document: {DocumentId})", _document.Id);

            // UIパネルをクリア
            CheckItemsContainer.Children.Clear();

            // チェック項目を再読み込み
            await _checkItemUIBuilder.BuildAsync(CheckItemsContainer, _document, PerformCaptureForCheckItem);

            _logger.LogInformation("チェック項目の再読み込みが完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェック項目の再読み込みに失敗しました");
            MessageBox.Show($"チェック項目の再読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// モニター移動ボタンクリック
    /// </summary>
    private async void MoveToNextMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var screens = WinForms.Screen.AllScreens;
            if (screens.Length <= 1)
            {
                _logger.LogInformation("モニターが1台のみのため移動できません");
                MessageBox.Show("モニターが1台のみのため移動できません。", "情報",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 現在のウィンドウ中心位置を取得
            var currentCenter = new System.Drawing.Point(
                (int)(Left + Width / 2),
                (int)(Top + Height / 2));

            // 現在のモニターを特定
            var currentScreen = WinForms.Screen.FromPoint(currentCenter);
            var currentIndex = Array.IndexOf(screens, currentScreen);

            // 次のモニターを取得（循環）
            var nextIndex = (currentIndex + 1) % screens.Length;
            var nextScreen = screens[nextIndex];

            _logger.LogInformation("モニター移動: {CurrentMonitor} → {NextMonitor}",
                currentScreen.DeviceName, nextScreen.DeviceName);

            // 次のモニターの作業領域に合わせてサイズと位置を調整
            await MoveToScreenAsync(nextScreen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "モニター移動に失敗しました");
            MessageBox.Show($"モニター移動に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Monitor Movement Methods

    /// <summary>
    /// 指定したモニターに移動し、サイズを調整
    /// </summary>
    private async Task MoveToScreenAsync(WinForms.Screen screen)
    {
        _isAdjustingPosition = true;
        try
        {
            // モニターの作業領域を取得（DPIスケーリング考慮）
            var workArea = GetScaledWorkArea(screen);

            // ChecklistWindow: 画面の1/3幅、右端に配置
            var checklistWidth = workArea.Width / 3.0;
            var checklistHeight = workArea.Height;
            var checklistLeft = workArea.Right - checklistWidth;
            var checklistTop = workArea.Top;

            Width = checklistWidth;
            Height = checklistHeight;
            Left = checklistLeft;
            Top = checklistTop;

            _logger.LogDebug("ChecklistWindow移動完了: サイズ={Width}x{Height}, 位置=({Left}, {Top})",
                checklistWidth, checklistHeight, checklistLeft, checklistTop);

            // ViewerWindow: 画面の2/3幅、左端に配置
            var viewerLeft = workArea.Left;
            var viewerTop = workArea.Top;
            var viewerWidth = workArea.Width * 2.0 / 3.0;
            var viewerHeight = workArea.Height;

            // 外部プログラム（Excel等）のハンドルを取得
            var externalHandle = _viewerWindow?.ExternalWindowHandle ?? IntPtr.Zero;
            var hasExternalProgram = externalHandle != IntPtr.Zero ||
                                     (_documentWindowHandle != IntPtr.Zero && (_viewerWindow == null || !_viewerWindow.IsVisible));

            _logger.LogInformation("移動対象確認: _viewerWindow={HasViewerWindow}, ExternalHandle={ExternalHandle}, _documentWindowHandle={DocHandle}, hasExternalProgram={HasExternal}",
                _viewerWindow != null, externalHandle, _documentWindowHandle, hasExternalProgram);

            if (hasExternalProgram)
            {
                // 外部プログラムを移動（ExternalHandleまたはdocumentWindowHandleを使用）
                var handleToMove = externalHandle != IntPtr.Zero ? externalHandle : _documentWindowHandle;
                var physicalWorkArea = screen.WorkingArea;
                await MoveExternalWindowAsync(handleToMove, physicalWorkArea);
            }
            else if (_viewerWindow != null && _viewerWindow.IsVisible)
            {
                // 内部ViewerWindowの場合のみSetPositionAndSizeを使用（PDFなど）
                _logger.LogInformation("ViewerWindow（内部）移動: Left={Left}, Top={Top}, Width={Width}, Height={Height}",
                    viewerLeft, viewerTop, viewerWidth, viewerHeight);

                _viewerWindow.SetPositionAndSize(viewerLeft, viewerTop, viewerWidth, viewerHeight);
            }
            else
            {
                _logger.LogWarning("移動対象のウィンドウがありません");
            }
        }
        finally
        {
            _isAdjustingPosition = false;
        }
    }

    /// <summary>
    /// DPIスケーリングを考慮した作業領域を取得
    /// </summary>
    private Rect GetScaledWorkArea(WinForms.Screen screen)
    {
        // WPFのDPIスケーリングを取得
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        // WinFormsの座標をWPF座標に変換
        var workArea = screen.WorkingArea;
        return new Rect(
            workArea.X / dpiX,
            workArea.Y / dpiY,
            workArea.Width / dpiX,
            workArea.Height / dpiY);
    }

    /// <summary>
    /// 外部ウィンドウを指定した作業領域に移動
    /// </summary>
    private async Task MoveExternalWindowAsync(IntPtr handle, System.Drawing.Rectangle workArea)
    {
        var viewerX = workArea.Left;
        var viewerY = workArea.Top;
        var viewerWidth = workArea.Width * 2 / 3;
        var viewerHeight = workArea.Height;

        _logger.LogInformation("外部ウィンドウ移動: Handle={Handle}, X={X}, Y={Y}, Width={Width}, Height={Height}",
            handle, viewerX, viewerY, viewerWidth, viewerHeight);

        // 最大化状態を解除
        if (IsZoomed(handle))
        {
            SendMessage(handle, WM_SYSCOMMAND, (IntPtr)SC_RESTORE, IntPtr.Zero);
            await Task.Delay(WINDOW_STATE_RESTORE_DELAY_MS);
        }

        // まず位置のみ移動（サイズは変更しない）
        // これによりウィンドウが移動先モニターに移動し、DPIコンテキストが更新される
        var moveResult = SetWindowPos(handle, IntPtr.Zero, viewerX, viewerY, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
        if (!moveResult)
        {
            var error = Marshal.GetLastWin32Error();
            _logger.LogWarning("SetWindowPos（位置移動）が失敗しました: ErrorCode={ErrorCode}", error);
        }
        _logger.LogInformation("位置移動完了（サイズ変更なし）: Result={Result}", moveResult);

        // Dispatcherで待機してメッセージポンプを回し、DPIコンテキスト更新を処理させる
        await Task.Delay(DPI_CONTEXT_UPDATE_DELAY_MS);
        // Dispatcherの処理を待つ
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

        // その後サイズを設定（移動先モニターのDPIが適用されるはず）
        var sizeResult = SetWindowPos(handle, IntPtr.Zero, viewerX, viewerY, viewerWidth, viewerHeight, SWP_NOZORDER);
        if (!sizeResult)
        {
            var error = Marshal.GetLastWin32Error();
            _logger.LogError("SetWindowPos（サイズ設定）が失敗しました: ErrorCode={ErrorCode}, X={X}, Y={Y}, Width={Width}, Height={Height}",
                error, viewerX, viewerY, viewerWidth, viewerHeight);
        }
        _logger.LogInformation("サイズ設定完了: Result={Result}, 設定値: X={X}, Y={Y}, Width={Width}, Height={Height}",
            sizeResult, viewerX, viewerY, viewerWidth, viewerHeight);
    }

    #endregion

    #region Capture Methods

    /// <summary>
    /// キャプチャ処理を実行
    /// </summary>
    private void PerformCapture()
    {
        bool continueCapture = true;

        while (continueCapture)
        {
            continueCapture = false;

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

                    using (var bitmap = _captureService.CaptureRectangle(rectangle))
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
                            continueCapture = true;
                        }
                    }

                    if (!continueCapture)
                    {
                        _logger.LogInformation("キャプチャ処理が完了しました");
                    }
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
                break;
            }
        }
    }

    /// <summary>
    /// System.Drawing.BitmapをWPF BitmapSourceに変換
    /// </summary>
    private BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
    {
        IntPtr hBitmap = IntPtr.Zero;
        try
        {
            hBitmap = bitmap.GetHbitmap();
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // BitmapSourceをフリーズして、GDIリソースを即座に解放できるようにする
            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }
        }
    }

    /// <summary>
    /// チェック項目に対してキャプチャ処理を実行
    /// </summary>
    private async Task PerformCaptureForCheckItem(CheckItemViewModel viewModel, UIElement checkBoxContainer)
    {
        bool continueCapture = true;

        while (continueCapture)
        {
            continueCapture = false;

            try
            {
                _logger.LogInformation("チェック項目のキャプチャを開始: {Path}", viewModel.Path);

                // キャプチャファイルパスを生成
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var capturesDir = Path.Combine(_documentRootPath, _pathSettings.CapturesDirectory, $"document_{_document.Id}");
                var fileName = $"checkitem_{viewModel.Entity.Id}_{timestamp}.png";
                var relativePath = Path.Combine(_pathSettings.CapturesDirectory, $"document_{_document.Id}", fileName);
                var absolutePath = Path.Combine(_documentRootPath, relativePath);

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

                    using (var bitmap = _captureService.CaptureRectangle(rectangle))
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
                            continueCapture = true;
                            continue;
                        }

                        // 保存が成功した場合
                        if (previewResult == true && !string.IsNullOrEmpty(previewWindow.SavedFilePath))
                        {
                            _logger.LogInformation("キャプチャ画像を保存: {Path}", relativePath);

                            // ViewModelを更新
                            viewModel.CaptureFilePath = relativePath;
                            viewModel.IsChecked = true;

                            // DBを更新
                            var linkedItem = await _checkItemDocumentRepository.GetByDocumentAndCheckItemAsync(
                                _document.Id, viewModel.Entity.Id);

                            if (linkedItem != null)
                            {
                                // 既存の紐づきがある場合は更新
                                await _checkItemDocumentRepository.UpdateCaptureFileAsync(linkedItem.Id, relativePath);
                                await _checkItemDocumentRepository.SaveChangesAsync();
                                _logger.LogInformation("DB更新完了: CheckItemDocument.Id={Id}, CaptureFile={Path}",
                                    linkedItem.Id, relativePath);
                            }
                            else
                            {
                                // 紐づきがない場合は新規作成
                                var newLink = new CheckItemDocument
                                {
                                    DocumentId = _document.Id,
                                    CheckItemId = viewModel.Entity.Id,
                                    LinkedAt = DateTime.UtcNow,
                                    CaptureFile = relativePath
                                };
                                await _checkItemDocumentRepository.AddAsync(newLink);
                                await _checkItemDocumentRepository.SaveChangesAsync();
                                _logger.LogInformation("新規紐づけ作成: DocumentId={DocumentId}, CheckItemId={CheckItemId}, CaptureFile={Path}",
                                    _document.Id, viewModel.Entity.Id, relativePath);
                            }

                            // UIを更新（チェックボックスとカメラアイコン）
                            if (checkBoxContainer is StackPanel stackPanel)
                            {
                                // StackPanelの1番目がCheckBox、2番目がButton（📷）
                                if (stackPanel.Children.Count >= 1 && stackPanel.Children[0] is CheckBox checkBox)
                                {
                                    checkBox.IsChecked = true;
                                    _logger.LogInformation("チェックボックスをオン");
                                }
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
                break;
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 入力ダイアログを表示
    /// </summary>
    /// <param name="title">ダイアログのタイトル</param>
    /// <param name="prompt">表示するプロンプト</param>
    /// <param name="hint">オプションのヒントテキスト</param>
    /// <returns>成功フラグと入力テキストのタプル</returns>
    private (bool Success, string InputText) ShowInputDialog(string title, string prompt, string? hint = null)
    {
        var inputDialog = new Window
        {
            Title = title,
            Width = hint != null ? 450 : 400,
            Height = hint != null ? 220 : 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var stackPanel = new StackPanel { Margin = new Thickness(20) };
        stackPanel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) });

        if (hint != null)
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = hint,
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 20) };
        stackPanel.Children.Add(textBox);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var cancelButton = new Button { Content = "キャンセル", Width = 80, IsCancel = true };

        okButton.Click += (s, args) => { inputDialog.DialogResult = true; inputDialog.Close(); };
        cancelButton.Click += (s, args) => { inputDialog.DialogResult = false; inputDialog.Close(); };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        stackPanel.Children.Add(buttonPanel);

        inputDialog.Content = stackPanel;
        textBox.Focus();

        bool? result = inputDialog.ShowDialog();

        return (result == true && !string.IsNullOrWhiteSpace(textBox.Text), textBox.Text?.Trim() ?? "");
    }

    #endregion
}
