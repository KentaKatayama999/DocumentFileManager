using System;
using System.Linq;
using System.Windows;

namespace DocumentFileManager.Viewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// アプリケーション起動時の処理
    /// </summary>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // コマンドライン引数からファイルパスを取得
        if (e.Args.Length == 0)
        {
            MessageBox.Show("使い方: DocumentFileManager.Viewer.exe <ファイルパス>", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var filePath = e.Args[0];

        try
        {
            // ViewerWindowを作成して表示
            var viewerWindow = new ViewerWindow(filePath);
            viewerWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ビューアーの起動に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}

