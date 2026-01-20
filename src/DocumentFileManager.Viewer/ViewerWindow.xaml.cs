using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace DocumentFileManager.Viewer;

/// <summary>
/// ViewerWindow.xaml の相互作用ロジック
/// </summary>
public partial class ViewerWindow : Window
{
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOACTIVATE = 0x0010;

    /// <summary>
    /// ファイルが開かれたときに発生するイベント（外部プログラムの場合は起動開始時）
    /// </summary>
    public event EventHandler<IntPtr>? FileOpened;

    /// <summary>
    /// 外部プログラムのウィンドウが準備完了したときに発生するイベント
    /// </summary>
    public event EventHandler<IntPtr>? ExternalWindowReady;

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    private readonly string _filePath;
    private bool _isAdjustingPosition = false;
    private IntPtr _externalWindowHandle = IntPtr.Zero;

    public ViewerWindow(string filePath)
    {
        _filePath = filePath;

        InitializeComponent();

        // ウィンドウタイトルにファイル名を設定
        Title = $"Document Viewer - {Path.GetFileName(filePath)}";

        // ウィンドウサイズを画面の2/3幅×全高に設定、左端に配置
        InitializeWindowPosition();

        // ウィンドウハンドル初期化時にWin32フックを設定
        SourceInitialized += ViewerWindow_SourceInitialized;

        // 位置と高さを固定するためのイベントハンドラ
        SizeChanged += ViewerWindow_SizeChanged;

        // ファイルを読み込み
        Loaded += ViewerWindow_Loaded;
    }

    /// <summary>
    /// ウィンドウハンドルが初期化されたときにWin32フックを設定
    /// </summary>
    private void ViewerWindow_SourceInitialized(object? sender, EventArgs e)
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
            handled = false;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// ウィンドウ位置を初期化（画面の2/3幅×全高、左端に配置）
    /// </summary>
    private void InitializeWindowPosition()
    {
        var workArea = SystemParameters.WorkArea;
        Width = workArea.Width * 2.0 / 3.0;  // 左2/3
        Height = workArea.Height;            // 全画面高さ
        Left = workArea.Left;                // 画面左端
        Top = workArea.Top;                  // 画面上端
    }

    /// <summary>
    /// サイズが変更されたときに高さを画面いっぱいに固定
    /// </summary>
    private void ViewerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
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

            // 左端に固定
            Left = workArea.Left;
            Top = workArea.Top;
        }
        finally
        {
            _isAdjustingPosition = false;
        }
    }

    /// <summary>
    /// ウィンドウが読み込まれたときにファイルを読み込み
    /// </summary>
    private async void ViewerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                MessageBox.Show($"ファイルが見つかりません:\n{_filePath}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            await LoadFile(_filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ファイルの読み込みに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    /// <summary>
    /// ファイルを読み込み（拡張子に応じて振り分け）
    /// </summary>
    private async Task LoadFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        System.Diagnostics.Debug.WriteLine($"[ViewerWindow.LoadFile] 開始: extension={extension}, IsSupportedFile={IsSupportedFile(extension)}, ShouldOpenWithDefault={ShouldOpenWithDefault(extension)}");

        if (IsSupportedFile(extension))
        {
            System.Diagnostics.Debug.WriteLine("[ViewerWindow.LoadFile] IsSupportedFile分岐に入りました");
            // Viewerで表示
            LoadInViewer(filePath, extension);

            // ViewerWindow自体のハンドルを使用
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            var handle = hwndSource?.Handle ?? IntPtr.Zero;
            System.Diagnostics.Debug.WriteLine($"[ViewerWindow.LoadFile] FileOpenedイベント発火: handle={handle}");
            FileOpened?.Invoke(this, handle);
        }
        else if (ShouldOpenWithDefault(extension))
        {
            System.Diagnostics.Debug.WriteLine("[ViewerWindow.LoadFile] ShouldOpenWithDefault分岐に入りました");
            // ウィンドウを完全に非表示
            Hide();

            // 先にファイルオープン完了イベントを発生（ChecklistWindowをすぐに開く）
            // ViewerWindow自体のハンドルを渡す（外部プログラムのハンドル取得は非同期で行う）
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            var viewerHandle = hwndSource?.Handle ?? IntPtr.Zero;
            System.Diagnostics.Debug.WriteLine($"[ViewerWindow.LoadFile] FileOpenedイベント発火（外部プログラム）: handle={viewerHandle}");
            FileOpened?.Invoke(this, viewerHandle);

            // Windows標準プログラムで開く（ハンドル取得は非同期で行う）
            _ = OpenWithDefaultProgramAsync(filePath);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ViewerWindow.LoadFile] 未対応のファイル形式");
            // 未知のファイル形式
            MessageBox.Show($"未対応のファイル形式です: {extension}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }

        // async メソッドのため、完了を示す
        await Task.CompletedTask;
    }

    /// <summary>
    /// サポート対象ファイルかどうかを判定
    /// </summary>
    private bool IsSupportedFile(string extension)
    {
        // Viewerで表示するファイル形式（画像、テキスト、PDF）
        return extension is ".png" or ".jpg" or ".jpeg" or ".gif"
            or ".txt" or ".log" or ".csv" or ".md"
            or ".pdf";
    }

    /// <summary>
    /// Windows標準プログラムで開くファイルかどうかを判定
    /// （ChecklistWindowは連携する）
    /// </summary>
    private bool ShouldOpenWithDefault(string extension)
    {
        // Email, Office, CADファイル（PDFは内部Viewerで表示）
        return extension is ".msg" or ".eml"
            or ".docx" or ".doc" or ".xlsx" or ".xls" or ".xlsm" or ".xlm" or ".pptx" or ".ppt"
            or ".3dm" or ".sldprt" or ".sldasm" or ".dwg" or ".igs" or ".iges";
    }

    /// <summary>
    /// 専用ビューアーで読み込み
    /// </summary>
    private void LoadInViewer(string filePath, string extension)
    {
        UserControl? viewer = extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" => CreateImageViewer(filePath),
            ".txt" or ".log" or ".csv" or ".md" => CreateTextViewer(filePath),
            ".pdf" => CreatePdfViewer(filePath),
            ".msg" => CreateEmailViewer(filePath),
            _ => null
        };

        if (viewer != null)
        {
            ViewerContent.Children.Clear();
            ViewerContent.Children.Add(viewer);
        }
        else
        {
            MessageBox.Show($"未対応のファイル形式です: {extension}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }

    /// <summary>
    /// 画像ビューアーを作成
    /// </summary>
    private UserControl? CreateImageViewer(string filePath)
    {
        var viewer = new Viewers.ImageViewer();
        viewer.LoadImage(filePath);
        return viewer;
    }

    /// <summary>
    /// テキストビューアーを作成
    /// </summary>
    private UserControl? CreateTextViewer(string filePath)
    {
        var viewer = new Viewers.TextViewer();
        viewer.LoadText(filePath);
        return viewer;
    }

    /// <summary>
    /// PDFビューアーを作成
    /// </summary>
    private UserControl? CreatePdfViewer(string filePath)
    {
        var viewer = new Viewers.PdfViewer();
        viewer.LoadPdf(filePath);
        return viewer;
    }

    /// <summary>
    /// メールビューアーを作成（暫定実装）
    /// </summary>
    private UserControl? CreateEmailViewer(string filePath)
    {
        // TODO: MsgReaderを使った専用のEmailViewer UserControlを実装
        MessageBox.Show("メール表示機能は未実装です。\nWindows標準プログラムで開きます。", "情報",
            MessageBoxButton.OK, MessageBoxImage.Information);
        _ = OpenWithDefaultProgram(filePath);
        return null;
    }

    /// <summary>
    /// Windows標準プログラムで開く（非同期ラッパー、fire-and-forget用）
    /// </summary>
    private async Task OpenWithDefaultProgramAsync(string filePath)
    {
        _externalWindowHandle = await OpenWithDefaultProgram(filePath);
    }

    /// <summary>
    /// Windows標準プログラムで開く（ウィンドウハンドルを取得）
    /// </summary>
    private async Task<IntPtr> OpenWithDefaultProgram(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLower();

            // メールファイルの場合は専用処理
            if (extension is ".msg" or ".eml")
            {
                _externalWindowHandle = OpenEmailFile(filePath);

                // ExternalWindowReadyイベントを発火（LoadingWindowを閉じるため）
                Dispatcher.Invoke(() => ExternalWindowReady?.Invoke(this, _externalWindowHandle));

                return _externalWindowHandle;
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            if (process == null)
            {
                MessageBox.Show("プロセスの起動に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return IntPtr.Zero;
            }

            // プロセスがUIの準備を完了するまで待機（最大10秒）
            process.WaitForInputIdle(10000);

            // ファイル名（拡張子なし）を取得
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            // 拡張子に対応するプロセス名を取得
            var targetProcessNames = GetProcessNamesForExtension(extension);

            // ポーリングでウィンドウハンドルを取得（最大2分間）
            const int maxWaitSeconds = 120;
            const int pollIntervalMs = 500;
            var maxAttempts = (maxWaitSeconds * 1000) / pollIntervalMs;

            IntPtr handle = IntPtr.Zero;
            var startTime = DateTime.Now;
            string? lastWindowTitle = null;
            int processId = 0;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    // 全プロセスから該当するウィンドウを検索
                    var allProcesses = Process.GetProcesses();
                    foreach (var proc in allProcesses.Where(p => targetProcessNames.Contains(p.ProcessName.ToLower())))
                    {
                        try
                        {
                            proc.Refresh();
                            var windowHandle = proc.MainWindowHandle;

                            if (windowHandle != IntPtr.Zero)
                            {
                                var windowTitle = proc.MainWindowTitle;

                                System.Diagnostics.Debug.WriteLine($"[ViewerWindow] 検出: Process={proc.ProcessName}, Title=\"{windowTitle}\", 探しているファイル=\"{fileNameWithoutExt}\"");

                                // ウィンドウタイトルにファイル名が含まれているかチェック
                                if (!string.IsNullOrEmpty(windowTitle) &&
                                    windowTitle.Contains(fileNameWithoutExt))
                                {
                                    // 成功
                                    handle = windowHandle;
                                    processId = proc.Id;
                                    lastWindowTitle = windowTitle;
                                    var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

                                    System.Diagnostics.Debug.WriteLine($"[ViewerWindow] マッチ成功！経過時間={elapsedSeconds:F1}秒");

                                    // ウィンドウを画面左2/3に配置・リサイズ
                                    PositionExternalWindow(handle);

                                    _externalWindowHandle = handle;

                                    // UIスレッドでExternalWindowReadyイベントを発火
                                    Dispatcher.Invoke(() => ExternalWindowReady?.Invoke(this, handle));

                                    return handle;
                                }

                                lastWindowTitle = windowTitle;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // プロセスが既に終了している可能性
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    // Continue searching
                }

                await Task.Delay(pollIntervalMs);
            }

            // タイムアウト
            var totalElapsed = (DateTime.Now - startTime).TotalSeconds;
            System.Diagnostics.Debug.WriteLine($"[ViewerWindow] タイムアウト: 経過時間={totalElapsed:F1}秒");

            // タイムアウトでもExternalWindowReadyイベントを発火（ハンドルはゼロ）
            Dispatcher.Invoke(() => ExternalWindowReady?.Invoke(this, IntPtr.Zero));

            return IntPtr.Zero;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ファイルを開けませんでした:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 拡張子に対応するプロセス名リストを取得
    /// </summary>
    private string[] GetProcessNamesForExtension(string extension)
    {
        return extension switch
        {
            ".docx" or ".doc" => new[] { "winword" },
            ".xlsx" or ".xls" or ".xlsm" or ".xlm" => new[] { "excel" },
            ".pptx" or ".ppt" => new[] { "powerpnt" },
            ".pdf" => new[] { "acrord32", "acrobat", "foxitreader", "msedge", "chrome" },
            ".msg" or ".eml" => new[] { "outlook" },
            ".3dm" => new[] { "rhino" },
            ".sldprt" or ".sldasm" => new[] { "sldworks" },
            ".dwg" => new[] { "acad" },
            ".igs" or ".iges" => new[] { "rhino", "sldworks", "acad" },
            _ => new[] { "" }
        };
    }

    /// <summary>
    /// 外部プログラムのウィンドウを画面左2/3に配置・リサイズ
    /// </summary>
    private bool PositionExternalWindow(IntPtr handle)
    {
        try
        {
            var workArea = SystemParameters.WorkArea;

            // 画面左2/3の領域を計算
            int x = (int)workArea.Left;
            int y = (int)workArea.Top;
            int width = (int)(workArea.Width * 2.0 / 3.0);
            int height = (int)workArea.Height;

            // 最大化を解除（最大化状態だとSetWindowPosが効かない）
            ShowWindow(handle, SW_RESTORE);

            // SetWindowPosでウィンドウを移動・リサイズ
            bool result = SetWindowPos(handle, IntPtr.Zero, x, y, width, height, 0);

            // ウィンドウを前面に表示
            SetForegroundWindow(handle);

            return result;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ウィンドウの位置調整に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    /// <summary>
    /// メールファイルをOutlookで開いてウィンドウハンドルを取得
    /// </summary>
    private IntPtr OpenEmailFile(string filePath)
    {
        try
        {
            // 既存のOutlookウィンドウを記録
            var existingOutlookWindows = GetOutlookWindowHandles();

            // メールファイルを開く
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            if (process == null)
            {
                MessageBox.Show("プロセスの起動に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return IntPtr.Zero;
            }

            // 新しいOutlookウィンドウを待機（最大30秒）
            return WaitForNewOutlookWindow(existingOutlookWindows, 30000, 500);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"メールファイルを開けませんでした:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 現在のOutlookウィンドウハンドルを取得
    /// </summary>
    private List<IntPtr> GetOutlookWindowHandles()
    {
        var outlookHandles = new List<IntPtr>();
        var allProcesses = Process.GetProcesses();

        foreach (var proc in allProcesses.Where(p => p.ProcessName.ToLower() == "outlook"))
        {
            try
            {
                proc.Refresh();
                var windowHandle = proc.MainWindowHandle;
                if (windowHandle != IntPtr.Zero)
                {
                    outlookHandles.Add(windowHandle);
                }
            }
            catch (InvalidOperationException)
            {
                // プロセスが既に終了している可能性
                continue;
            }
        }

        return outlookHandles;
    }

    /// <summary>
    /// 新しいOutlookウィンドウを待機して取得
    /// </summary>
    private IntPtr WaitForNewOutlookWindow(List<IntPtr> existingWindows, int maxWaitMs, int intervalMs)
    {
        var startTime = DateTime.Now;
        var maxAttempts = maxWaitMs / intervalMs;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var currentOutlookWindows = GetOutlookWindowHandles();
            var newWindows = currentOutlookWindows.Except(existingWindows).ToList();

            if (newWindows.Any())
            {
                // 最新のウィンドウを返す
                var latestWindow = newWindows.Last();
                PositionExternalWindow(latestWindow);
                return latestWindow;
            }

            System.Threading.Thread.Sleep(intervalMs);
        }

        // タイムアウト: 既存の最新Outlookウィンドウを返す
        if (existingWindows.Any())
        {
            var latestExisting = existingWindows.Last();
            PositionExternalWindow(latestExisting);
            return latestExisting;
        }

        MessageBox.Show("Outlookウィンドウの取得に失敗しました", "タイムアウト",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return IntPtr.Zero;
    }

    /// <summary>
    /// 開いたファイルのウィンドウハンドルを取得
    /// 外部プログラムで開いた場合はそのハンドル、内部ビューアーで開いた場合はViewerWindow自体のハンドル
    /// </summary>
    public IntPtr GetWindowHandle()
    {
        if (_externalWindowHandle != IntPtr.Zero)
        {
            return _externalWindowHandle;
        }

        // ViewerWindow自体のハンドルを返す
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        return hwndSource?.Handle ?? IntPtr.Zero;
    }

    /// <summary>
    /// ウィンドウの位置とサイズを設定（外部からの呼び出し用）
    /// 移動ブロックを一時的に解除して設定
    /// </summary>
    public void SetPositionAndSize(double left, double top, double width, double height)
    {
        _isAdjustingPosition = true;
        try
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
        finally
        {
            _isAdjustingPosition = false;
        }
    }

    /// <summary>
    /// 外部プログラムのウィンドウハンドルを取得
    /// </summary>
    public IntPtr ExternalWindowHandle => _externalWindowHandle;
}
