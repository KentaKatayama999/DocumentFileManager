using System.Windows;

namespace DocumentFileManager.UI.Helpers;

/// <summary>
/// ウィンドウ位置計算ヘルパー（Issue #3: 配置ボタンでViewerWindow連動）
/// </summary>
public static class WindowPositionCalculator
{
    /// <summary>
    /// ChecklistWindowを左に配置した場合のViewerWindow位置を計算
    /// </summary>
    /// <param name="workArea">作業領域</param>
    /// <param name="checklistWindowWidth">ChecklistWindowの幅</param>
    /// <returns>ViewerWindowの位置とサイズ (X, Y, Width, Height)</returns>
    public static (int X, int Y, int Width, int Height) CalculateViewerPositionForLeftDock(
        Rect workArea, double checklistWindowWidth)
    {
        int viewerX = (int)(workArea.Left + checklistWindowWidth);
        int viewerY = (int)workArea.Top;
        int viewerWidth = (int)(workArea.Width - checklistWindowWidth);
        int viewerHeight = (int)workArea.Height;

        return (viewerX, viewerY, viewerWidth, viewerHeight);
    }

    /// <summary>
    /// ChecklistWindowを右に配置した場合のViewerWindow位置を計算
    /// </summary>
    /// <param name="workArea">作業領域</param>
    /// <param name="checklistWindowWidth">ChecklistWindowの幅</param>
    /// <returns>ViewerWindowの位置とサイズ (X, Y, Width, Height)</returns>
    public static (int X, int Y, int Width, int Height) CalculateViewerPositionForRightDock(
        Rect workArea, double checklistWindowWidth)
    {
        int viewerX = (int)workArea.Left;
        int viewerY = (int)workArea.Top;
        int viewerWidth = (int)(workArea.Width - checklistWindowWidth);
        int viewerHeight = (int)workArea.Height;

        return (viewerX, viewerY, viewerWidth, viewerHeight);
    }

    /// <summary>
    /// ChecklistWindowを左に配置した場合の位置を計算
    /// </summary>
    /// <param name="workArea">作業領域</param>
    /// <returns>ChecklistWindowの位置 (Left, Top)</returns>
    public static (double Left, double Top) CalculateChecklistPositionForLeftDock(Rect workArea)
    {
        return (workArea.Left, workArea.Top);
    }

    /// <summary>
    /// ChecklistWindowを右に配置した場合の位置を計算
    /// </summary>
    /// <param name="workArea">作業領域</param>
    /// <param name="checklistWindowWidth">ChecklistWindowの幅</param>
    /// <returns>ChecklistWindowの位置 (Left, Top)</returns>
    public static (double Left, double Top) CalculateChecklistPositionForRightDock(
        Rect workArea, double checklistWindowWidth)
    {
        return (workArea.Right - checklistWindowWidth, workArea.Top);
    }
}
