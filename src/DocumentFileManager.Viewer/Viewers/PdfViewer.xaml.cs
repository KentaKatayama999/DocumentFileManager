using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DocumentFileManager.Viewer.Viewers;

/// <summary>
/// PdfViewer.xaml の相互作用ロジック
/// </summary>
public partial class PdfViewer : UserControl
{
    public PdfViewer()
    {
        InitializeComponent();
        InitializeWebView();
    }

    /// <summary>
    /// WebView2を初期化
    /// </summary>
    private async void InitializeWebView()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WebView2の初期化に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// PDFファイルを読み込み
    /// </summary>
    public async void LoadPdf(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("PDFファイルが見つかりません", filePath);
            }

            // WebView2の初期化を待つ
            await WebView.EnsureCoreWebView2Async();

            // PDFファイルをWebView2で開く
            WebView.CoreWebView2.Navigate(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDFファイルの読み込みに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
