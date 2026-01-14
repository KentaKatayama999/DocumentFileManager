using System.Windows;
using DocumentFileManager.UI.Services.Abstractions;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// WPF用のダイアログ表示サービス実装
/// MessageBoxをラップし、UIスレッドでの実行を保証する
/// WinFormsホストからの呼び出しにも対応
/// </summary>
public class WpfDialogService : IDialogService
{
    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string message, string title)
    {
        // WinFormsホストの場合、Application.Currentがnullになる
        if (Application.Current?.Dispatcher != null)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }

        // WinFormsホストまたはDispatcherが利用不可の場合は直接呼び出し
        var directResult = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        return await Task.FromResult(directResult == MessageBoxResult.Yes);
    }

    /// <inheritdoc/>
    public async Task<Abstractions.DialogResult> ShowYesNoCancelAsync(string message, string title)
    {
        // WinFormsホストの場合、Application.Currentがnullになる
        if (Application.Current?.Dispatcher != null)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                return result switch
                {
                    MessageBoxResult.Yes => Abstractions.DialogResult.Yes,
                    MessageBoxResult.No => Abstractions.DialogResult.No,
                    _ => Abstractions.DialogResult.Cancel
                };
            });
        }

        // WinFormsホストまたはDispatcherが利用不可の場合は直接呼び出し
        var directResult = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return await Task.FromResult(directResult switch
        {
            MessageBoxResult.Yes => Abstractions.DialogResult.Yes,
            MessageBoxResult.No => Abstractions.DialogResult.No,
            _ => Abstractions.DialogResult.Cancel
        });
    }

    /// <inheritdoc/>
    public async Task ShowInformationAsync(string message, string title)
    {
        // WinFormsホストの場合、Application.Currentがnullになる
        if (Application.Current?.Dispatcher != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
            return;
        }

        // WinFormsホストまたはDispatcherが利用不可の場合は直接呼び出し
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string message, string title)
    {
        // WinFormsホストの場合、Application.Currentがnullになる
        if (Application.Current?.Dispatcher != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
            return;
        }

        // WinFormsホストまたはDispatcherが利用不可の場合は直接呼び出し
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        await Task.CompletedTask;
    }
}
