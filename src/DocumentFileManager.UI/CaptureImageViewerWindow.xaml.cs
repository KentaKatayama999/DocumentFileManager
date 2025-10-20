using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI;

/// <summary>
/// キャプチャ画像ビューアウィンドウ
/// </summary>
public partial class CaptureImageViewerWindow : Window
{
    private readonly string _imagePath;
    private readonly ILogger<CaptureImageViewerWindow>? _logger;

    // 拡大縮小用の変数
    private double _scale = 1.0;
    private const double ScaleRate = 1.1;
    private const double MinScale = 0.1;
    private const double MaxScale = 10.0;

    // ドラッグ用の変数
    private bool _isDragging = false;
    private Point _dragStartPoint;

    // 画像の元のサイズ
    private double _initialImageWidth;
    private double _initialImageHeight;

    /// <summary>
    /// 画像が削除されたかどうか
    /// </summary>
    public bool IsDeleted { get; private set; }

    public CaptureImageViewerWindow(string imagePath, ILogger<CaptureImageViewerWindow>? logger = null)
    {
        _imagePath = imagePath ?? throw new ArgumentNullException(nameof(imagePath));
        _logger = logger;

        InitializeComponent();

        LoadImage();
    }

    /// <summary>
    /// 画像を読み込んで表示
    /// </summary>
    private void LoadImage()
    {
        try
        {
            if (!File.Exists(_imagePath))
            {
                _logger?.LogWarning("画像ファイルが見つかりません: {ImagePath}", _imagePath);
                MessageBox.Show(
                    $"画像ファイルが見つかりません:\n{_imagePath}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(_imagePath, UriKind.Absolute);
            bitmap.EndInit();

            CaptureImage.Source = bitmap;

            // 画像の元のサイズを保存
            _initialImageWidth = bitmap.PixelWidth;
            _initialImageHeight = bitmap.PixelHeight;

            // 画像のサイズを設定
            CaptureImage.Width = _initialImageWidth;
            CaptureImage.Height = _initialImageHeight;

            // Gridのサイズを画像のサイズに設定
            ImageContainer.Width = _initialImageWidth;
            ImageContainer.Height = _initialImageHeight;

            _logger?.LogInformation("キャプチャ画像を読み込みました: {ImagePath} (Size: {Width}x{Height})",
                _imagePath, _initialImageWidth, _initialImageHeight);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "画像の読み込みに失敗しました: {ImagePath}", _imagePath);
            MessageBox.Show(
                $"画像の読み込みに失敗しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }

    /// <summary>
    /// 削除ボタンクリック
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show(
                "このキャプチャ画像を削除しますか？",
                "削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (File.Exists(_imagePath))
                {
                    File.Delete(_imagePath);
                    IsDeleted = true;

                    _logger?.LogInformation("キャプチャ画像を削除しました: {ImagePath}", _imagePath);

                    MessageBox.Show(
                        "キャプチャ画像を削除しました",
                        "削除完了",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "画像の削除に失敗しました: {ImagePath}", _imagePath);
            MessageBox.Show(
                $"画像の削除に失敗しました:\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 閉じるボタンクリック
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// リセットボタンクリック
    /// </summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetZoom();
    }

    /// <summary>
    /// マウスホイールで拡大縮小
    /// </summary>
    private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 拡大前のスクロール位置の割合を計算
        double oldScrollableWidth = ImageScrollViewer.ScrollableWidth;
        double oldScrollableHeight = ImageScrollViewer.ScrollableHeight;
        double horizontalRatio = oldScrollableWidth > 0 ? ImageScrollViewer.HorizontalOffset / oldScrollableWidth : 0.5;
        double verticalRatio = oldScrollableHeight > 0 ? ImageScrollViewer.VerticalOffset / oldScrollableHeight : 0.5;

        // 拡大縮小率を計算
        double oldScale = _scale;
        if (e.Delta > 0)
        {
            _scale *= ScaleRate;
        }
        else
        {
            _scale /= ScaleRate;
        }

        // 拡大率の制限
        _scale = Math.Max(MinScale, Math.Min(MaxScale, _scale));

        // 画像とGridのサイズを更新
        double newWidth = _initialImageWidth * _scale;
        double newHeight = _initialImageHeight * _scale;

        CaptureImage.Width = newWidth;
        CaptureImage.Height = newHeight;
        ImageContainer.Width = newWidth;
        ImageContainer.Height = newHeight;

        // レイアウト更新を強制
        ImageScrollViewer.UpdateLayout();

        // スクロール位置を調整（拡大前の割合を維持）
        if (ImageScrollViewer.ScrollableWidth > 0)
        {
            ImageScrollViewer.ScrollToHorizontalOffset(horizontalRatio * ImageScrollViewer.ScrollableWidth);
        }
        if (ImageScrollViewer.ScrollableHeight > 0)
        {
            ImageScrollViewer.ScrollToVerticalOffset(verticalRatio * ImageScrollViewer.ScrollableHeight);
        }

        _logger?.LogDebug("画像を拡大縮小: Scale={Scale}, Size={Width}x{Height}", _scale, newWidth, newHeight);

        e.Handled = true;
    }

    /// <summary>
    /// 拡大縮小をリセット
    /// </summary>
    private void ResetZoom()
    {
        _scale = 1.0;

        // 画像とGridのサイズを元に戻す
        CaptureImage.Width = _initialImageWidth;
        CaptureImage.Height = _initialImageHeight;
        ImageContainer.Width = _initialImageWidth;
        ImageContainer.Height = _initialImageHeight;

        // ScrollViewerを左上にリセット
        ImageScrollViewer.ScrollToHorizontalOffset(0);
        ImageScrollViewer.ScrollToVerticalOffset(0);

        _logger?.LogInformation("画像表示をリセットしました");
    }

    /// <summary>
    /// マウスドラッグ開始（右クリック）
    /// </summary>
    private void ImageContainer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (CaptureImage.Source != null)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(ImageScrollViewer);
            ImageContainer.CaptureMouse();
            ImageContainer.Cursor = Cursors.Hand;
        }
    }

    /// <summary>
    /// マウスドラッグ終了（右クリック）
    /// </summary>
    private void ImageContainer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ImageContainer.ReleaseMouseCapture();
            ImageContainer.Cursor = Cursors.Arrow;
        }
    }

    /// <summary>
    /// マウス移動（ドラッグ中）
    /// </summary>
    private void ImageContainer_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && e.RightButton == MouseButtonState.Pressed)
        {
            Point currentPosition = e.GetPosition(ImageScrollViewer);
            Vector offset = currentPosition - _dragStartPoint;

            // ScrollViewerのオフセットを更新
            ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - offset.X);
            ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - offset.Y);

            _dragStartPoint = currentPosition;
        }
    }

    /// <summary>
    /// マウスがGridから離れた
    /// </summary>
    private void ImageContainer_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ImageContainer.ReleaseMouseCapture();
            ImageContainer.Cursor = Cursors.Arrow;
        }
    }
}
