using System.IO;

namespace DocumentFileManager.UI.Configuration;

/// <summary>
/// アプリケーションで使用する各種パス設定を保持するクラス。
/// </summary>
public class PathSettings
{
    /// <summary>ログ出力用のフォルダ。</summary>
    public string LogsFolder { get; set; } = "logs";

    /// <summary>SQLite データベースのファイル名。</summary>
    public string DatabaseName { get; set; } = "workspace.db";

    /// <summary>設定ファイルを配置するサブディレクトリ。</summary>
    public string ConfigDirectory { get; set; } = "config";

    /// <summary>資料ファイルを配置するサブディレクトリ。</summary>
    public string DocumentsDirectory { get; set; } = "documents";

    /// <summary>ローカルに保持するチェックリスト定義ファイル。</summary>
    public string ChecklistFile { get; set; } = Path.Combine("config", "checklist.json");

    /// <summary>現在選択されているチェックリストファイル（相対パス）。</summary>
    public string SelectedChecklistFile { get; set; } = Path.Combine("config", "checklist.json");

    /// <summary>チェックリスト定義の探索元フォルダ。未設定時はプロジェクトルートを使用。</summary>
    public string ChecklistDefinitionsFolder { get; set; } = string.Empty;

    /// <summary>appsettings.json などの設定ファイル名。</summary>
    public string SettingsFile { get; set; } = "appsettings.json";

    /// <summary>キャプチャ画像を格納するサブディレクトリ。</summary>
    public string CapturesDirectory { get; set; } = "captures";

    /// <summary>
    /// プロジェクトルートと相対/絶対パスを結合して絶対パスを得る。
    /// すでに絶対パスの場合はそのまま返す。
    /// </summary>
    public string ToAbsolutePath(string projectRoot, string relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
        {
            return projectRoot;
        }

        return Path.IsPathRooted(relativeOrAbsolute)
            ? relativeOrAbsolute
            : Path.Combine(projectRoot, relativeOrAbsolute);
    }
}
