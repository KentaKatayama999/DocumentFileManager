namespace DocumentFileManager.UI.Services.Abstractions;

/// <summary>
/// ダイアログ表示の結果を表す列挙型
/// </summary>
public enum DialogResult
{
    /// <summary>「はい」が選択された</summary>
    Yes,
    /// <summary>「いいえ」が選択された</summary>
    No,
    /// <summary>「キャンセル」が選択された</summary>
    Cancel
}

/// <summary>
/// ダイアログ表示サービスのインターフェース
/// MessageBoxを抽象化し、テスト可能にする
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 確認ダイアログを表示（はい/いいえ）
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="title">ダイアログのタイトル</param>
    /// <returns>「はい」が選択された場合true、「いいえ」の場合false</returns>
    Task<bool> ShowConfirmationAsync(string message, string title);

    /// <summary>
    /// 3択ダイアログを表示（はい/いいえ/キャンセル）
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="title">ダイアログのタイトル</param>
    /// <returns>選択された結果</returns>
    Task<DialogResult> ShowYesNoCancelAsync(string message, string title);

    /// <summary>
    /// 情報ダイアログを表示
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="title">ダイアログのタイトル</param>
    Task ShowInformationAsync(string message, string title);

    /// <summary>
    /// エラーダイアログを表示
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="title">ダイアログのタイトル</param>
    Task ShowErrorAsync(string message, string title);
}
