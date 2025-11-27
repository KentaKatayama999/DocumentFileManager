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
    private bool _isInitialized = false;
    private string? _pendingFilePath = null;

    public PdfViewer()
    {
        InitializeComponent();
        Loaded += PdfViewer_Loaded;
    }

    /// <summary>
    /// コントロールがロードされたときにWebView2を初期化
    /// </summary>
    private async void PdfViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;

        try
        {
            System.Diagnostics.Debug.WriteLine("[PdfViewer] Loaded イベント開始");

            // WebView2の環境オプションを設定
            var options = new CoreWebView2EnvironmentOptions();
            options.AdditionalBrowserArguments = "--allow-file-access-from-files --disable-web-security";

            System.Diagnostics.Debug.WriteLine("[PdfViewer] WebView2環境を作成中...");
            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);

            System.Diagnostics.Debug.WriteLine("[PdfViewer] EnsureCoreWebView2Async開始...");
            await WebView.EnsureCoreWebView2Async(environment);

            System.Diagnostics.Debug.WriteLine("[PdfViewer] WebView2初期化完了");
            _isInitialized = true;

            // 保留中のファイルがあれば読み込み
            if (!string.IsNullOrEmpty(_pendingFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[PdfViewer] 保留ファイルを読み込み: {_pendingFilePath}");
                NavigateToPdf(_pendingFilePath);
                _pendingFilePath = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewer] エラー: {ex.Message}");
            MessageBox.Show($"WebView2の初期化に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// PDFファイルを読み込み
    /// </summary>
    public void LoadPdf(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show($"PDFファイルが見つかりません:\n{filePath}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_isInitialized)
        {
            NavigateToPdf(filePath);
        }
        else
        {
            // 初期化完了まで保留
            _pendingFilePath = filePath;
        }
    }

    /// <summary>
    /// PDFファイルに移動
    /// </summary>
    private void NavigateToPdf(string filePath)
    {
        try
        {
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
