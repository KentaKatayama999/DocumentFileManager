using System;
using System.IO;
using System.Windows;
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

            _logger?.LogInformation("キャプチャ画像を読み込みました: {ImagePath}", _imagePath);
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
}
