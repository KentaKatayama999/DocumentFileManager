using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

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
            // WebView2の環境オプションを設定
            var options = new CoreWebView2EnvironmentOptions();
            options.AdditionalBrowserArguments = "--allow-file-access-from-files --disable-web-security";

            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await WebView.EnsureCoreWebView2Async(environment);
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

            // ローカルファイルパスをfile:// URIに変換
            var uri = new Uri(filePath, UriKind.Absolute);
            var fileUri = uri.AbsoluteUri;

            // PDFファイルをWebView2で開く
            WebView.CoreWebView2.Navigate(fileUri);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDFファイルの読み込みに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
