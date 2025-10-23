using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DocumentFileManager.UI.Windows;

/// <summary>
/// ImagePreviewWindow.xaml の相互作用ロジック
/// </summary>
public partial class ImagePreviewWindow : Window
{
    private readonly BitmapSource _capturedImage;
    private readonly string? _autoSavePath;

    /// <summary>
    /// 再キャプチャが要求されたかどうか
    /// </summary>
    public bool RecaptureRequested { get; private set; }

    /// <summary>
    /// 保存されたファイルのパス（自動保存モード時に設定される）
    /// </summary>
    public string? SavedFilePath { get; private set; }

    public ImagePreviewWindow(BitmapSource capturedImage, string? autoSavePath = null)
    {
        _capturedImage = capturedImage ?? throw new ArgumentNullException(nameof(capturedImage));
        _autoSavePath = autoSavePath;

        InitializeComponent();

        // 画像を表示
        PreviewImage.Source = _capturedImage;
    }

    /// <summary>
    /// 保存ボタンクリック
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string filePath;

            // 自動保存モード時は指定されたパスに保存
            if (!string.IsNullOrEmpty(_autoSavePath))
            {
                filePath = _autoSavePath;

                // ディレクトリが存在しない場合は作成
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                SaveImage(filePath);
                SavedFilePath = filePath;

                MessageBox.Show($"画像を保存しました:\n{filePath}", "保存完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 保存後はウィンドウを閉じる
                DialogResult = true;
                Close();
            }
            else
            {
                // 通常モード時はダイアログで保存先を選択
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG画像 (*.png)|*.png|JPEG画像 (*.jpg)|*.jpg|BMP画像 (*.bmp)|*.bmp",
                    FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    DefaultExt = ".png"
                };

                if (dialog.ShowDialog() == true)
                {
                    filePath = dialog.FileName;
                    SaveImage(filePath);
                    SavedFilePath = filePath;

                    MessageBox.Show($"画像を保存しました:\n{filePath}", "保存完了",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // 保存後はウィンドウを閉じる
                    DialogResult = true;
                    Close();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"画像の保存に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 再キャプチャボタンクリック
    /// </summary>
    private void RecaptureButton_Click(object sender, RoutedEventArgs e)
    {
        RecaptureRequested = true;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 終了ボタンクリック
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        RecaptureRequested = false;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 画像をファイルに保存
    /// </summary>
    private void SaveImage(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        BitmapEncoder encoder = extension switch
        {
            ".png" => new PngBitmapEncoder(),
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
            ".bmp" => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };

        encoder.Frames.Add(BitmapFrame.Create(_capturedImage));

        using var stream = new FileStream(filePath, FileMode.Create);
        encoder.Save(stream);
    }
}
