using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DocumentFileManager.Viewer.Viewers;

/// <summary>
/// ImageViewer.xaml の相互作用ロジック
/// </summary>
public partial class ImageViewer : UserControl, INotifyPropertyChanged
{
    private const double ZoomMin = 0.1;
    private const double ZoomMax = 10.0;
    private const double ZoomStep = 0.2;

    private double _zoom = 1.0;
    private Point? _lastMousePosition;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (Math.Abs(_zoom - value) > 0.001)
            {
                _zoom = Math.Clamp(value, ZoomMin, ZoomMax);
                OnPropertyChanged();
                UpdateZoom();
            }
        }
    }

    public ImageViewer()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// 画像ファイルを読み込み
    /// </summary>
    public void LoadImage(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("画像ファイルが見つかりません", filePath);
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            ImageControl.Source = bitmap;

            // 画像サイズに合わせて初期表示
            Zoom = 1.0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"画像の読み込みに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ズームを更新
    /// </summary>
    private void UpdateZoom()
    {
        ScaleTransform.ScaleX = _zoom;
        ScaleTransform.ScaleY = _zoom;
    }

    /// <summary>
    /// マウスホイールでズーム
    /// </summary>
    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Delta > 0)
            {
                Zoom += ZoomStep;
            }
            else
            {
                Zoom -= ZoomStep;
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// ドラッグ開始
    /// </summary>
    private void ImageControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ImageControl.IsMouseCaptured) return;

        _lastMousePosition = e.GetPosition(ScrollViewer);
        ImageControl.CaptureMouse();
        ImageControl.Cursor = Cursors.ScrollAll;
    }

    /// <summary>
    /// ドラッグ終了
    /// </summary>
    private void ImageControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ImageControl.ReleaseMouseCapture();
        ImageControl.Cursor = Cursors.Hand;
        _lastMousePosition = null;
    }

    /// <summary>
    /// ドラッグ中（パン移動）
    /// </summary>
    private void ImageControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (!ImageControl.IsMouseCaptured || !_lastMousePosition.HasValue) return;

        var currentPosition = e.GetPosition(ScrollViewer);
        var delta = currentPosition - _lastMousePosition.Value;

        ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset - delta.X);
        ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - delta.Y);

        _lastMousePosition = currentPosition;
    }

    /// <summary>
    /// 100%表示ボタン
    /// </summary>
    private void ZoomActual_Click(object sender, RoutedEventArgs e)
    {
        Zoom = 1.0;
    }

    /// <summary>
    /// 拡大ボタン
    /// </summary>
    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        Zoom += ZoomStep;
    }

    /// <summary>
    /// 縮小ボタン
    /// </summary>
    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        Zoom -= ZoomStep;
    }

    /// <summary>
    /// 画面に合わせるボタン
    /// </summary>
    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        if (ImageControl.Source == null) return;

        var imageWidth = ImageControl.Source.Width;
        var imageHeight = ImageControl.Source.Height;
        var viewWidth = ScrollViewer.ActualWidth;
        var viewHeight = ScrollViewer.ActualHeight;

        var scaleX = viewWidth / imageWidth;
        var scaleY = viewHeight / imageHeight;

        Zoom = Math.Min(scaleX, scaleY) * 0.95; // 95%に調整して余白を確保
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
