using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// 画面キャプチャ機能を提供するサービス
/// </summary>
public class ScreenCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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
        // すべてのスクリーンを含む仮想スクリーンのサイズを計算
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
    /// </summary>
    private Bitmap CaptureRectangle(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    /// <summary>
    /// ビットマップをPNGファイルとして保存
    /// </summary>
    /// <param name="bitmap">保存するビットマップ</param>
    /// <param name="filePath">保存先のファイルパス</param>
    public void SaveBitmap(Bitmap bitmap, string filePath)
    {
        // ディレクトリが存在しない場合は作成
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
    /// <param name="captureType">キャプチャの種類</param>
    /// <param name="filePath">保存先のファイルパス</param>
    /// <param name="windowHandle">ウィンドウキャプチャの場合のウィンドウハンドル</param>
    /// <returns>キャプチャが成功した場合true</returns>
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screen capture failed: {ex.Message}");
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
