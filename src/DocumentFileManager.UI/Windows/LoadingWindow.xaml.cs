using System.Windows;

namespace DocumentFileManager.UI.Windows;

/// <summary>
/// 外部アプリケーション起動中の読み込み画面
/// </summary>
public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ファイル名を設定
    /// </summary>
    public void SetFileName(string fileName)
    {
        FileNameText.Text = fileName;
    }

    /// <summary>
    /// アプリケーション名を設定
    /// </summary>
    public void SetApplicationName(string appName)
    {
        TitleText.Text = $"{appName} を起動しています...";
    }
}
