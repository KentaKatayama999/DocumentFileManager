using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Models;
using DocumentFileManager.UI.Services.Abstractions;
using DocumentFileManager.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// チェックリスト状態管理サービスの実装
/// チェック項目の状態遷移とDB操作を一元管理する
/// </summary>
public class ChecklistStateManager : IChecklistStateManager
{
    private readonly ICheckItemDocumentRepository _repository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ChecklistStateManager> _logger;

    public ChecklistStateManager(
        ICheckItemDocumentRepository repository,
        IDialogService dialogService,
        ILogger<ChecklistStateManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CheckItemTransition?> HandleCheckOnAsync(CheckItemViewModel viewModel, Document document)
    {
        try
        {
            _logger.LogDebug("チェックON処理開始: CheckItemId={CheckItemId}, DocumentId={DocumentId}",
                viewModel.Id, document.Id);

            // 現在の状態から遷移オブジェクトを作成
            var transition = await CreateTransitionAsync(viewModel, document);

            // 既存キャプチャがある場合の処理
            if (!string.IsNullOrEmpty(transition.OriginalRecord?.CaptureFile))
            {
                _logger.LogDebug("既存キャプチャあり: {CaptureFile}", transition.OriginalRecord.CaptureFile);

                // 復帰確認ダイアログを表示
                var result = await _dialogService.ShowYesNoCancelAsync(
                    "既存のキャプチャ画像が見つかりました。復帰しますか？\n\n" +
                    "はい: 既存のキャプチャを復帰\n" +
                    "いいえ: キャプチャを破棄して新規開始\n" +
                    "キャンセル: 操作を取り消し",
                    "キャプチャ復帰確認");

                switch (result)
                {
                    case DialogResult.Yes:
                        // 既存キャプチャを復帰 → 状態11
                        transition.RestoreTo11();
                        _logger.LogInformation("既存キャプチャを復帰しました");
                        break;

                    case DialogResult.No:
                        // キャプチャを破棄 → 状態10
                        transition.TransitionTo10();
                        _logger.LogInformation("既存キャプチャを破棄しました");
                        break;

                    case DialogResult.Cancel:
                        // キャンセル → ロールバック（nullを返す）
                        _logger.LogInformation("チェックON操作がキャンセルされました");
                        return null;
                }
            }
            else
            {
                // 既存キャプチャなし → 状態10へ遷移
                transition.TransitionTo10();
                _logger.LogDebug("状態10へ遷移");
            }

            return transition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックON処理中にエラーが発生しました");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CheckItemTransition> HandleCheckOffAsync(CheckItemViewModel viewModel, Document document)
    {
        try
        {
            _logger.LogDebug("チェックOFF処理開始: CheckItemId={CheckItemId}, DocumentId={DocumentId}",
                viewModel.Id, document.Id);

            // 現在の状態から遷移オブジェクトを作成
            var transition = await CreateTransitionAsync(viewModel, document);

            // チェックOFF時の遷移（キャプチャの有無に応じて20または22）
            transition.TransitionToOff();

            _logger.LogDebug("状態{State}へ遷移", transition.TargetState);

            return transition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "チェックOFF処理中にエラーが発生しました");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<CheckItemTransition> CommitCaptureAsync(CheckItemTransition transition, string captureFilePath)
    {
        try
        {
            _logger.LogDebug("キャプチャ保存: {CaptureFilePath}", captureFilePath);

            // キャプチャファイルパスを設定して状態11へ遷移
            transition.TransitionTo11(captureFilePath);

            _logger.LogInformation("キャプチャを保存しました: {CaptureFilePath}", captureFilePath);

            return Task.FromResult(transition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "キャプチャ保存中にエラーが発生しました");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CommitTransitionAsync(CheckItemTransition transition)
    {
        try
        {
            _logger.LogDebug("状態遷移コミット開始: CheckItemId={CheckItemId}, DocumentId={DocumentId}, State={State}",
                transition.CheckItemId, transition.DocumentId, transition.TargetState);

            if (transition.OriginalRecord == null)
            {
                // 新規レコード作成
                var newRecord = new CheckItemDocument
                {
                    CheckItemId = transition.CheckItemId,
                    DocumentId = transition.DocumentId,
                    IsChecked = transition.IsChecked ?? false,
                    CaptureFile = transition.CaptureFile,
                    LinkedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(newRecord);
                _logger.LogInformation("新規CheckItemDocumentレコードを作成しました");
            }
            else
            {
                // 既存レコード更新
                transition.OriginalRecord.IsChecked = transition.IsChecked ?? false;
                transition.OriginalRecord.CaptureFile = transition.CaptureFile;

                await _repository.UpdateAsync(transition.OriginalRecord);
                _logger.LogInformation("CheckItemDocumentレコードを更新しました: Id={Id}", transition.OriginalRecord.Id);
            }

            await _repository.SaveChangesAsync();
            transition.IsCommitted = true;

            _logger.LogDebug("状態遷移コミット完了");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状態遷移コミット中にエラーが発生しました");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task RollbackTransitionAsync(CheckItemTransition transition, CheckItemViewModel viewModel)
    {
        try
        {
            _logger.LogDebug("状態遷移ロールバック開始");

            // 遷移オブジェクトをロールバック
            transition.Rollback();

            // ViewModelの状態を元に戻す
            viewModel.IsChecked = transition.OriginalRecord?.IsChecked ?? false;
            viewModel.CaptureFilePath = transition.OriginalRecord?.CaptureFile;

            _logger.LogInformation("状態遷移をロールバックしました");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状態遷移ロールバック中にエラーが発生しました");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CheckItemTransition> CreateTransitionAsync(CheckItemViewModel viewModel, Document document)
    {
        try
        {
            // 既存レコードを取得
            var existingRecord = await _repository.GetByDocumentAndCheckItemAsync(document.Id, viewModel.Id);

            // 遷移オブジェクトを作成
            var transition = CheckItemTransition.Create(viewModel.Id, document.Id, existingRecord);

            _logger.LogDebug("遷移オブジェクト作成: OriginalState={OriginalState}", transition.OriginalState);

            return transition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "遷移オブジェクト作成中にエラーが発生しました");
            throw;
        }
    }
}
