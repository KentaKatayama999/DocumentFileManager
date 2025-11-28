using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// 画面キャプチャ機能を提供するサービス
/// マルチモニター環境で異なるDPI設定に対応
/// </summary>
public class ScreenCaptureService
{
    #region Win32 API

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    private const uint SRCCOPY = 0x00CC0020;
    private const int ENUM_CURRENT_SETTINGS = -1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    #endregion

    /// <summary>
    /// プライマリモニター全体をキャプチャ
    /// </summary>
    public Bitmap CapturePrimaryScreen()
    {
        var bounds = WinFormsScreen.PrimaryScreen!.Bounds;
        return CaptureRectangle(bounds);
    }

    /// <summary>
    /// すべてのモニターを含む全画面をキャプチャ
    /// </summary>
    public Bitmap CaptureAllScreens()
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (var screen in WinFormsScreen.AllScreens)
        {
            minX = Math.Min(minX, screen.Bounds.Left);
            minY = Math.Min(minY, screen.Bounds.Top);
            maxX = Math.Max(maxX, screen.Bounds.Right);
            maxY = Math.Max(maxY, screen.Bounds.Bottom);
        }

        var bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        return CaptureRectangle(bounds);
    }

    /// <summary>
    /// 現在アクティブなウィンドウをキャプチャ
    /// </summary>
    public Bitmap? CaptureActiveWindow()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        return CaptureWindow(handle);
    }

    /// <summary>
    /// 指定されたウィンドウハンドルのウィンドウをキャプチャ
    /// </summary>
    public Bitmap? CaptureWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }

        if (!GetWindowRect(windowHandle, out RECT rect))
        {
            return null;
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var bounds = new Rectangle(rect.Left, rect.Top, width, height);
        return CaptureRectangle(bounds);
    }

    /// <summary>
    /// 指定された矩形領域をキャプチャ
    /// DPI仮想化環境でも正確な物理座標でキャプチャを行う
    /// </summary>
    public Bitmap CaptureRectangle(Rectangle bounds)
    {
        // キャプチャ対象のモニターを特定し、DPIスケールを取得
        var centerPoint = new Point(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);
        var targetScreen = WinFormsScreen.FromPoint(centerPoint);
        var dpiScale = GetDpiScaleForScreen(targetScreen);

        // モニター内の相対座標を計算し、DPIスケールで変換
        var relativeX = bounds.X - targetScreen.Bounds.X;
        var relativeY = bounds.Y - targetScreen.Bounds.Y;

        var scaledX = targetScreen.Bounds.X + (int)(relativeX * dpiScale);
        var scaledY = targetScreen.Bounds.Y + (int)(relativeY * dpiScale);
        var scaledWidth = (int)(bounds.Width * dpiScale);
        var scaledHeight = (int)(bounds.Height * dpiScale);

        // BitBltで物理座標を使用してキャプチャ
        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, scaledWidth, scaledHeight);
        IntPtr hOld = SelectObject(hdcMem, hBitmap);

        BitBlt(hdcMem, 0, 0, scaledWidth, scaledHeight, hdcScreen, scaledX, scaledY, SRCCOPY);

        var bitmap = Image.FromHbitmap(hBitmap);

        // リソース解放
        SelectObject(hdcMem, hOld);
        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        return bitmap;
    }

    /// <summary>
    /// 指定されたスクリーンのDPIスケールを取得
    /// EnumDisplaySettingsで物理解像度を取得し、仮想解像度との比率を計算
    /// </summary>
    private double GetDpiScaleForScreen(WinFormsScreen screen)
    {
        try
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();

            if (EnumDisplaySettings(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                var physicalWidth = devMode.dmPelsWidth;
                var physicalHeight = devMode.dmPelsHeight;

                var scaleX = (double)physicalWidth / screen.Bounds.Width;
                var scaleY = (double)physicalHeight / screen.Bounds.Height;
                return Math.Max(scaleX, scaleY);
            }
        }
        catch
        {
            // フォールバック
        }

        return 1.0;
    }

    /// <summary>
    /// ビットマップをPNGファイルとして保存
    /// </summary>
    public void SaveBitmap(Bitmap bitmap, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        bitmap.Save(filePath, ImageFormat.Png);
    }

    /// <summary>
    /// キャプチャを実行してファイルに保存
    /// </summary>
    public bool CaptureAndSave(CaptureType captureType, string filePath, IntPtr windowHandle = default)
    {
        try
        {
            Bitmap? bitmap = captureType switch
            {
                CaptureType.PrimaryScreen => CapturePrimaryScreen(),
                CaptureType.AllScreens => CaptureAllScreens(),
                CaptureType.ActiveWindow => CaptureActiveWindow(),
                CaptureType.SpecificWindow => CaptureWindow(windowHandle),
                _ => throw new ArgumentException($"Unknown capture type: {captureType}")
            };

            if (bitmap == null)
            {
                return false;
            }

            using (bitmap)
            {
                SaveBitmap(bitmap, filePath);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 一時的なキャプチャファイル名を生成
    /// </summary>
    public string GenerateTempCaptureFileName()
    {
        return $"capture_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
    }
}

/// <summary>
/// キャプチャの種類
/// </summary>
public enum CaptureType
{
    /// <summary>プライマリモニター</summary>
    PrimaryScreen,

    /// <summary>すべてのモニター</summary>
    AllScreens,

    /// <summary>アクティブウィンドウ</summary>
    ActiveWindow,

    /// <summary>特定のウィンドウ</summary>
    SpecificWindow
}
