using System.Windows;
using DocumentFileManager.UI.Helpers;
using Xunit;

namespace DocumentFileManager.Tests.Helpers;

/// <summary>
/// WindowPositionCalculatorのテスト（Issue #3: 配置ボタンでViewerWindow連動）
/// </summary>
public class WindowPositionCalculatorTests
{
    // 一般的なフルHDディスプレイの作業領域（タスクバー除く）
    private static readonly Rect StandardWorkArea = new Rect(0, 0, 1920, 1040);
    private const double StandardChecklistWidth = 400;

    [Fact]
    public void CalculateViewerPositionForLeftDock_ViewerWindowが右側に配置される()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (x, y, width, height) = WindowPositionCalculator.CalculateViewerPositionForLeftDock(
            workArea, checklistWidth);

        // Assert
        Assert.Equal(400, x);  // ChecklistWindowの右端
        Assert.Equal(0, y);    // 画面上端
        Assert.Equal(1520, width);  // 残りの幅 (1920 - 400)
        Assert.Equal(1040, height); // 作業領域の高さ
    }

    [Fact]
    public void CalculateViewerPositionForRightDock_ViewerWindowが左側に配置される()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (x, y, width, height) = WindowPositionCalculator.CalculateViewerPositionForRightDock(
            workArea, checklistWidth);

        // Assert
        Assert.Equal(0, x);    // 画面左端
        Assert.Equal(0, y);    // 画面上端
        Assert.Equal(1520, width);  // 残りの幅 (1920 - 400)
        Assert.Equal(1040, height); // 作業領域の高さ
    }

    [Fact]
    public void CalculateChecklistPositionForLeftDock_ChecklistWindowが左端に配置される()
    {
        // Arrange
        var workArea = StandardWorkArea;

        // Act
        var (left, top) = WindowPositionCalculator.CalculateChecklistPositionForLeftDock(workArea);

        // Assert
        Assert.Equal(0, left);
        Assert.Equal(0, top);
    }

    [Fact]
    public void CalculateChecklistPositionForRightDock_ChecklistWindowが右端に配置される()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (left, top) = WindowPositionCalculator.CalculateChecklistPositionForRightDock(
            workArea, checklistWidth);

        // Assert
        Assert.Equal(1520, left);  // 1920 - 400
        Assert.Equal(0, top);
    }

    [Fact]
    public void LeftDock_ChecklistWindowとViewerWindowが重ならない()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (checklistLeft, _) = WindowPositionCalculator.CalculateChecklistPositionForLeftDock(workArea);
        var (viewerX, _, viewerWidth, _) = WindowPositionCalculator.CalculateViewerPositionForLeftDock(
            workArea, checklistWidth);

        // Assert - ChecklistWindowの右端がViewerWindowの左端と一致
        var checklistRight = checklistLeft + checklistWidth;
        Assert.Equal(checklistRight, viewerX);
    }

    [Fact]
    public void RightDock_ChecklistWindowとViewerWindowが重ならない()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (checklistLeft, _) = WindowPositionCalculator.CalculateChecklistPositionForRightDock(
            workArea, checklistWidth);
        var (viewerX, _, viewerWidth, _) = WindowPositionCalculator.CalculateViewerPositionForRightDock(
            workArea, checklistWidth);

        // Assert - ViewerWindowの右端がChecklistWindowの左端と一致
        var viewerRight = viewerX + viewerWidth;
        Assert.Equal(viewerRight, checklistLeft);
    }

    [Fact]
    public void LeftDock_画面全体をカバーする()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (checklistLeft, _) = WindowPositionCalculator.CalculateChecklistPositionForLeftDock(workArea);
        var (viewerX, _, viewerWidth, _) = WindowPositionCalculator.CalculateViewerPositionForLeftDock(
            workArea, checklistWidth);

        // Assert - ChecklistWindow + ViewerWindow = 画面幅
        var totalWidth = checklistWidth + viewerWidth;
        Assert.Equal(workArea.Width, totalWidth);
    }

    [Fact]
    public void RightDock_画面全体をカバーする()
    {
        // Arrange
        var workArea = StandardWorkArea;
        var checklistWidth = StandardChecklistWidth;

        // Act
        var (_, _, viewerWidth, _) = WindowPositionCalculator.CalculateViewerPositionForRightDock(
            workArea, checklistWidth);

        // Assert - ChecklistWindow + ViewerWindow = 画面幅
        var totalWidth = checklistWidth + viewerWidth;
        Assert.Equal(workArea.Width, totalWidth);
    }

    [Fact]
    public void 異なる解像度でも正しく計算される_4K()
    {
        // Arrange - 4Kディスプレイ（タスクバー除く）
        var workArea = new Rect(0, 0, 3840, 2100);
        var checklistWidth = 500.0;

        // Act
        var (viewerX, viewerY, viewerWidth, viewerHeight) =
            WindowPositionCalculator.CalculateViewerPositionForLeftDock(workArea, checklistWidth);

        // Assert
        Assert.Equal(500, viewerX);
        Assert.Equal(0, viewerY);
        Assert.Equal(3340, viewerWidth);  // 3840 - 500
        Assert.Equal(2100, viewerHeight);
    }

    [Fact]
    public void 異なる解像度でも正しく計算される_HD()
    {
        // Arrange - HDディスプレイ（タスクバー除く）
        var workArea = new Rect(0, 0, 1366, 728);
        var checklistWidth = 350.0;

        // Act
        var (viewerX, viewerY, viewerWidth, viewerHeight) =
            WindowPositionCalculator.CalculateViewerPositionForRightDock(workArea, checklistWidth);

        // Assert
        Assert.Equal(0, viewerX);
        Assert.Equal(0, viewerY);
        Assert.Equal(1016, viewerWidth);  // 1366 - 350
        Assert.Equal(728, viewerHeight);
    }

    [Fact]
    public void 作業領域がオフセットされている場合も正しく計算される()
    {
        // Arrange - 左側にサイドバーがある場合など
        var workArea = new Rect(100, 50, 1820, 990);  // Left=100, Top=50
        var checklistWidth = 400.0;

        // Act
        var (viewerX, viewerY, _, _) =
            WindowPositionCalculator.CalculateViewerPositionForLeftDock(workArea, checklistWidth);
        var (checklistLeft, checklistTop) =
            WindowPositionCalculator.CalculateChecklistPositionForLeftDock(workArea);

        // Assert
        Assert.Equal(100, checklistLeft);  // workArea.Left
        Assert.Equal(50, checklistTop);    // workArea.Top
        Assert.Equal(500, viewerX);        // workArea.Left + checklistWidth
        Assert.Equal(50, viewerY);         // workArea.Top
    }
}
