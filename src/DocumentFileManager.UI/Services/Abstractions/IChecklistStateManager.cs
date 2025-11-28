using DocumentFileManager.Entities;
using DocumentFileManager.UI.Models;
using DocumentFileManager.UI.ViewModels;

namespace DocumentFileManager.UI.Services.Abstractions;

/// <summary>
/// チェックリスト状態管理サービスのインターフェース
/// チェック項目の状態遷移とDB操作を一元管理する
/// </summary>
public interface IChecklistStateManager
{
    /// <summary>
    /// チェックON時の処理を行う
    /// - 未紐づけの場合: キャプチャ確認ダイアログを表示
    /// - 既存キャプチャがある場合: 復帰確認ダイアログを表示
    /// </summary>
    /// <param name="viewModel">対象のCheckItemViewModel</param>
    /// <param name="document">現在編集中のDocument</param>
    /// <returns>状態遷移情報（キャンセル時はnull）</returns>
    Task<CheckItemTransition?> HandleCheckOnAsync(CheckItemViewModel viewModel, Document document);

    /// <summary>
    /// チェックOFF時の処理を行う
    /// - キャプチャありの場合: 状態22へ遷移
    /// - キャプチャなしの場合: 状態20へ遷移
    /// </summary>
    /// <param name="viewModel">対象のCheckItemViewModel</param>
    /// <param name="document">現在編集中のDocument</param>
    /// <returns>状態遷移情報</returns>
    Task<CheckItemTransition> HandleCheckOffAsync(CheckItemViewModel viewModel, Document document);

    /// <summary>
    /// キャプチャ保存をコミットする
    /// </summary>
    /// <param name="transition">状態遷移情報</param>
    /// <param name="captureFilePath">保存したキャプチャファイルパス</param>
    /// <returns>更新された状態遷移情報</returns>
    Task<CheckItemTransition> CommitCaptureAsync(CheckItemTransition transition, string captureFilePath);

    /// <summary>
    /// 状態遷移をDBにコミットする
    /// </summary>
    /// <param name="transition">状態遷移情報</param>
    Task CommitTransitionAsync(CheckItemTransition transition);

    /// <summary>
    /// 状態遷移をロールバックする
    /// </summary>
    /// <param name="transition">状態遷移情報</param>
    /// <param name="viewModel">対象のCheckItemViewModel（UIを元に戻すため）</param>
    Task RollbackTransitionAsync(CheckItemTransition transition, CheckItemViewModel viewModel);

    /// <summary>
    /// 現在の状態から遷移オブジェクトを作成する
    /// </summary>
    /// <param name="viewModel">対象のCheckItemViewModel</param>
    /// <param name="document">現在編集中のDocument</param>
    /// <returns>状態遷移情報</returns>
    Task<CheckItemTransition> CreateTransitionAsync(CheckItemViewModel viewModel, Document document);
}
