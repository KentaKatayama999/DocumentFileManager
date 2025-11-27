using System.IO;
using System.Windows;
using DocumentFileManager.UI;

namespace DocumentFileManager.Sample;

/// <summary>
/// DocumentFileManager.UI ライブラリの動作確認用サンプルアプリケーション
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // WPF Application を作成
        var app = new Application();

        // ProjectRoot フォルダのパスを取得（実行ファイルと同じ階層）
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = Path.Combine(baseDir, "ProjectRoot");

        // ProjectRoot フォルダが存在しない場合は作成
        if (!Directory.Exists(projectRoot))
        {
            Directory.CreateDirectory(projectRoot);
        }

        // MainWindow を表示
        DocumentFileManagerHost.ShowMainWindow(projectRoot);
    }
}
