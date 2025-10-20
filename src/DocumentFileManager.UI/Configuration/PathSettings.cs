namespace DocumentFileManager.UI.Configuration;

/// <summary>
/// アプリケーション内で使用する各種パス設定
/// </summary>
public class PathSettings
{
    /// <summary>
    /// ログファイルの出力フォルダ名
    /// </summary>
    public string LogsFolder { get; set; } = "Logs";

    /// <summary>
    /// SQLiteデータベースファイル名
    /// </summary>
    public string DatabaseName { get; set; } = "workspace.db";

    /// <summary>
    /// チェックリスト定義JSONファイル名（デフォルト値）
    /// </summary>
    public string ChecklistFile { get; set; } = "checklist.json";

    /// <summary>
    /// 選択されたチェックリスト定義JSONファイル名
    /// </summary>
    public string SelectedChecklistFile { get; set; } = "checklist.json";

    /// <summary>
    /// 設定ファイル名
    /// </summary>
    public string SettingsFile { get; set; } = "appsettings.json";

    /// <summary>
    /// キャプチャ画像の保存フォルダ名
    /// </summary>
    public string CapturesDirectory { get; set; } = "captures";

    /// <summary>
    /// 資料ファイルの保存フォルダ名
    /// PDF、Office文書、CADファイルなどを保存するディレクトリ
    /// </summary>
    public string DocumentsDirectory { get; set; } = "test-files";

    /// <summary>
    /// プロジェクトルートまでの階層数（実行ファイルから何階層上がるか）
    /// bin/Debug/net9.0-windows から 5階層上がソリューションルート
    /// </summary>
    public int ProjectRootLevelsUp { get; set; } = 5;
}
