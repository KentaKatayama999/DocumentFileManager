using DocumentFileManager.Entities;

namespace DocumentFileManager.UI.Models;

/// <summary>
/// チェック項目の状態遷移を管理するクラス
/// DBへのコミットを遅延させ、キャンセル時のロールバックを可能にする
/// </summary>
public class CheckItemTransition
{
    /// <summary>
    /// 元の表示状態（ロールバック用）
    /// 形式: "XY" (X=チェックボックス状態, Y=カメラボタン状態)
    /// 00: 未紐づけ
    /// 10: チェックON、キャプチャなし
    /// 11: チェックON、キャプチャあり
    /// 20: チェックOFF（履歴あり）、キャプチャなし
    /// 22: チェックOFF（履歴あり）、キャプチャあり
    /// </summary>
    public string OriginalState { get; set; } = "00";

    /// <summary>
    /// 元のCheckItemDocumentレコード（存在する場合）
    /// </summary>
    public CheckItemDocument? OriginalRecord { get; set; }

    /// <summary>
    /// 遷移後の表示状態
    /// </summary>
    public string TargetState { get; set; } = "00";

    /// <summary>
    /// 遷移後のチェック状態
    /// </summary>
    public bool? IsChecked { get; set; }

    /// <summary>
    /// 遷移後のキャプチャファイルパス
    /// </summary>
    public string? CaptureFile { get; set; }

    /// <summary>
    /// 確定済みフラグ
    /// </summary>
    public bool IsCommitted { get; set; } = false;

    /// <summary>
    /// CheckItemId
    /// </summary>
    public int CheckItemId { get; set; }

    /// <summary>
    /// DocumentId（現在編集中の資料ID）
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// 現在の状態からCheckItemTransitionを作成する
    /// </summary>
    /// <param name="checkItemId">チェック項目ID</param>
    /// <param name="documentId">資料ID</param>
    /// <param name="existingRecord">既存のCheckItemDocumentレコード（存在する場合）</param>
    /// <returns>CheckItemTransition</returns>
    public static CheckItemTransition Create(int checkItemId, int documentId, CheckItemDocument? existingRecord)
    {
        var transition = new CheckItemTransition
        {
            CheckItemId = checkItemId,
            DocumentId = documentId,
            OriginalRecord = existingRecord
        };

        if (existingRecord == null)
        {
            // レコードなし → 00
            transition.OriginalState = "00";
        }
        else if (existingRecord.IsChecked)
        {
            // チェックON
            transition.OriginalState = string.IsNullOrEmpty(existingRecord.CaptureFile) ? "10" : "11";
        }
        else
        {
            // チェックOFF（履歴あり）
            transition.OriginalState = string.IsNullOrEmpty(existingRecord.CaptureFile) ? "20" : "22";
        }

        transition.TargetState = transition.OriginalState;
        transition.IsChecked = existingRecord?.IsChecked;
        transition.CaptureFile = existingRecord?.CaptureFile;

        return transition;
    }

    /// <summary>
    /// 状態を10（チェックON、キャプチャなし）に遷移
    /// </summary>
    public void TransitionTo10()
    {
        TargetState = "10";
        IsChecked = true;
        CaptureFile = null;
    }

    /// <summary>
    /// 状態を11（チェックON、キャプチャあり）に遷移
    /// </summary>
    /// <param name="captureFilePath">キャプチャファイルパス</param>
    public void TransitionTo11(string captureFilePath)
    {
        TargetState = "11";
        IsChecked = true;
        CaptureFile = captureFilePath;
    }

    /// <summary>
    /// 既存のキャプチャを復帰して11に遷移（現在のドキュメントのキャプチャ）
    /// </summary>
    public void RestoreTo11()
    {
        if (OriginalRecord != null && !string.IsNullOrEmpty(OriginalRecord.CaptureFile))
        {
            TargetState = "11";
            IsChecked = true;
            CaptureFile = OriginalRecord.CaptureFile;
        }
    }

    /// <summary>
    /// 指定したキャプチャを使用して11に遷移（他のドキュメントのキャプチャを復帰する場合）
    /// </summary>
    /// <param name="captureFilePath">復帰するキャプチャファイルパス</param>
    public void RestoreTo11WithCapture(string captureFilePath)
    {
        TargetState = "11";
        IsChecked = true;
        CaptureFile = captureFilePath;
    }

    /// <summary>
    /// 状態を20（チェックOFF、キャプチャなし）に遷移
    /// </summary>
    public void TransitionTo20()
    {
        TargetState = "20";
        IsChecked = false;
        // CaptureFileは維持しない
        CaptureFile = null;
    }

    /// <summary>
    /// 状態を22（チェックOFF、キャプチャあり）に遷移
    /// </summary>
    public void TransitionTo22()
    {
        TargetState = "22";
        IsChecked = false;
        // CaptureFileは維持
    }

    /// <summary>
    /// チェックOFF時の遷移（キャプチャの有無に応じて20または22）
    /// </summary>
    public void TransitionToOff()
    {
        IsChecked = false;
        TargetState = string.IsNullOrEmpty(CaptureFile) ? "20" : "22";
    }

    /// <summary>
    /// 元の状態に戻す（ロールバック）
    /// </summary>
    public void Rollback()
    {
        TargetState = OriginalState;
        IsChecked = OriginalRecord?.IsChecked;
        CaptureFile = OriginalRecord?.CaptureFile;
        IsCommitted = false;
    }

    /// <summary>
    /// 状態変更があるか確認
    /// </summary>
    public bool HasChanges => OriginalState != TargetState ||
                              OriginalRecord?.IsChecked != IsChecked ||
                              OriginalRecord?.CaptureFile != CaptureFile;

    /// <summary>
    /// 新規レコード作成が必要か
    /// </summary>
    public bool RequiresNewRecord => OriginalRecord == null ||
                                     OriginalRecord.DocumentId != DocumentId;
}
