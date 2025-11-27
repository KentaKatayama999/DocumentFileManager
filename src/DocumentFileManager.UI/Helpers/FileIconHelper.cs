using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DocumentFileManager.UI.Helpers;

/// <summary>
/// ファイル拡張子に関連付けられたアイコンを取得するヘルパークラス
/// </summary>
public static class FileIconHelper
{
    // キャッシュ（拡張子ごとにアイコンをキャッシュ）
    private static readonly Dictionary<string, ImageSource> _iconCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// ファイル拡張子からアイコンを取得
    /// </summary>
    /// <param name="extension">ファイル拡張子（例: ".pdf"）</param>
    /// <returns>アイコンのImageSource</returns>
    public static ImageSource? GetIconByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return null;

        // キャッシュにあれば返す
        if (_iconCache.TryGetValue(extension, out var cachedIcon))
            return cachedIcon;

        try
        {
            // Shell APIでアイコンを取得
            var shFileInfo = new SHFILEINFO();
            var flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;

            var result = SHGetFileInfo(
                extension,
                FILE_ATTRIBUTE_NORMAL,
                ref shFileInfo,
                (uint)Marshal.SizeOf(shFileInfo),
                flags);

            if (result == IntPtr.Zero || shFileInfo.hIcon == IntPtr.Zero)
                return null;

            // アイコンをWPF用のImageSourceに変換
            var icon = Icon.FromHandle(shFileInfo.hIcon);
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // アイコンハンドルを解放
            DestroyIcon(shFileInfo.hIcon);

            // フリーズして別スレッドからもアクセス可能に
            imageSource.Freeze();

            // キャッシュに保存
            _iconCache[extension] = imageSource;

            return imageSource;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// キャッシュをクリア
    /// </summary>
    public static void ClearCache()
    {
        _iconCache.Clear();
    }

    #region Win32 API

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    #endregion
}
