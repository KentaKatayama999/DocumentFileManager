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
        _repository = repository;
        _dialogService = dialogService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<CheckItemTransition?> HandleCheckOnAsync(CheckItemViewModel viewModel, Document document)
    {
        // TDD: Part 2で実装
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<CheckItemTransition> HandleCheckOffAsync(CheckItemViewModel viewModel, Document document)
    {
        // TDD: Part 2で実装
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<CheckItemTransition> CommitCaptureAsync(CheckItemTransition transition, string captureFilePath)
    {
        // TDD: Part 2で実装
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task CommitTransitionAsync(CheckItemTransition transition)
    {
        // TDD: Part 2で実装
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task RollbackTransitionAsync(CheckItemTransition transition, CheckItemViewModel viewModel)
    {
        // TDD: Part 2で実装
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<CheckItemTransition> CreateTransitionAsync(CheckItemViewModel viewModel, Document document)
    {
        // TDD: Part 2で実装
        throw new NotImplementedException();
    }
}
