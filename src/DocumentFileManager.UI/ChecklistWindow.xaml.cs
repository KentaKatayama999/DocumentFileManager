using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DocumentFileManager.Entities;
using DocumentFileManager.UI.Helpers;
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

    private readonly CheckItemUIBuilder _checkItemUIBuilder;
    private readonly ILogger<ChecklistWindow> _logger;
    private readonly Document _document;
    private bool _isDockingRight = true; // デフォルトは右端
    private bool _isAdjustingPosition = false; // 位置調整中フラグ

    public ChecklistWindow(
        Document document,
        CheckItemUIBuilder checkItemUIBuilder,
        ILogger<ChecklistWindow> logger)
    {
        _document = document;
        _checkItemUIBuilder = checkItemUIBuilder;
        _logger = logger;

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
            await _checkItemUIBuilder.BuildAsync(CheckItemsContainer, _document);

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
}
