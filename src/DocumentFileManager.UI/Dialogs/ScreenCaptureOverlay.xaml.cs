using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentFileManager.UI.Dialogs;

/// <summary>
/// 画面キャプチャの範囲選択オーバーレイ
/// </summary>
public partial class ScreenCaptureOverlay : Window
{
    private Point _startPoint;
    private bool _isSelecting;
    private Rect? _initialArea;

    /// <summary>
    /// 選択された矩形領域（スクリーン座標）
    /// </summary>
    public Rect? SelectedArea { get; private set; }

    public ScreenCaptureOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初期選択範囲を指定するコンストラクタ
    /// </summary>
    /// <param name="initialArea">初期選択範囲（スクリーン座標）</param>
    public ScreenCaptureOverlay(Rect initialArea) : this()
    {
        _initialArea = initialArea;
        Loaded += ScreenCaptureOverlay_Loaded;
    }

    /// <summary>
    /// ウィンドウが読み込まれたときに初期選択範囲を表示
    /// </summary>
    private void ScreenCaptureOverlay_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialArea.HasValue)
        {
            // スクリーン座標をウィンドウ座標に変換
            var topLeft = PointFromScreen(new Point(_initialArea.Value.Left, _initialArea.Value.Top));

            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, topLeft.X);
            Canvas.SetTop(SelectionRectangle, topLeft.Y);
            SelectionRectangle.Width = _initialArea.Value.Width;
            SelectionRectangle.Height = _initialArea.Value.Height;

            // 初期選択範囲を設定
            _startPoint = topLeft;
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(this);

            SelectionRectangle.Visibility = Visibility.Visible;
            System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            System.Windows.Controls.Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            var currentPoint = e.GetPosition(this);

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            System.Windows.Controls.Canvas.SetLeft(SelectionRectangle, x);
            System.Windows.Controls.Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;

            var currentPoint = e.GetPosition(this);

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
}
