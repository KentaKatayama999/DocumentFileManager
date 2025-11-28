using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentFileManager.UI.Dialogs;

/// <summary>
/// 画面キャプチャの範囲選択オーバーレイ
/// ViewerWindowの範囲に配置され、選択範囲をくり抜いて表示
/// </summary>
public partial class ScreenCaptureOverlay : Window
{
    private Point _startPoint;
    private bool _isSelecting;
    private readonly Rect? _targetArea;

    /// <summary>
    /// 選択された矩形領域（スクリーン座標）
    /// </summary>
    public Rect? SelectedArea { get; private set; }

    public ScreenCaptureOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// ターゲットウィンドウの範囲を指定するコンストラクタ
    /// </summary>
    /// <param name="targetArea">ターゲットウィンドウの範囲（スクリーン座標）</param>
    public ScreenCaptureOverlay(Rect targetArea) : this()
    {
        _targetArea = targetArea;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_targetArea.HasValue)
        {
            // オーバーレイウィンドウをターゲットウィンドウの位置・サイズに合わせる
            Left = _targetArea.Value.Left;
            Top = _targetArea.Value.Top;
            Width = _targetArea.Value.Width;
            Height = _targetArea.Value.Height;
        }
        else
        {
            // ターゲットがない場合はプライマリスクリーン全体
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                Left = primaryScreen.Bounds.Left;
                Top = primaryScreen.Bounds.Top;
                Width = primaryScreen.Bounds.Width;
                Height = primaryScreen.Bounds.Height;
            }
        }

        // オーバーレイ全体のサイズをジオメトリに設定
        FullAreaGeometry.Rect = new Rect(0, 0, Width, Height);
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(this);

            // 選択開始時は選択範囲を0にリセット
            UpdateSelection(new Rect(_startPoint.X, _startPoint.Y, 0, 0));
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            var currentPoint = e.GetPosition(this);

            // ウィンドウ範囲内にクランプ
            currentPoint = ClampToWindow(currentPoint);

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            UpdateSelection(new Rect(x, y, width, height));
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;

            var currentPoint = e.GetPosition(this);

            // ウィンドウ範囲内にクランプ
            currentPoint = ClampToWindow(currentPoint);

            // 選択範囲が小さすぎる場合はキャンセル
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            if (width < 10 || height < 10)
            {
                DialogResult = false;
                Close();
                return;
            }

            // ウィンドウ座標をスクリーン座標に変換
            var topLeft = PointToScreen(new Point(
                Math.Min(_startPoint.X, currentPoint.X),
                Math.Min(_startPoint.Y, currentPoint.Y)
            ));

            SelectedArea = new Rect(topLeft.X, topLeft.Y, width, height);
            DialogResult = true;
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// 選択範囲を更新（くり抜き表示）
    /// </summary>
    private void UpdateSelection(Rect selectionRect)
    {
        // くり抜き用のジオメトリを更新
        SelectionGeometry.Rect = selectionRect;

        // 枠線を更新
        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, selectionRect.X);
        Canvas.SetTop(SelectionBorder, selectionRect.Y);
        SelectionBorder.Width = Math.Max(0, selectionRect.Width);
        SelectionBorder.Height = Math.Max(0, selectionRect.Height);
    }

    /// <summary>
    /// 座標をウィンドウ範囲内にクランプ
    /// </summary>
    private Point ClampToWindow(Point point)
    {
        var x = Math.Max(0, Math.Min(Width, point.X));
        var y = Math.Max(0, Math.Min(Height, point.Y));
        return new Point(x, y);
    }
}
