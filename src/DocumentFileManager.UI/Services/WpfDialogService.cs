using System.Windows;
using DocumentFileManager.UI.Services.Abstractions;

namespace DocumentFileManager.UI.Services;

/// <summary>
/// WPF用のダイアログ表示サービス実装
/// MessageBoxをラップし、UIスレッドでの実行を保証する
/// </summary>
public class WpfDialogService : IDialogService
{
    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string message, string title)
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

    /// <inheritdoc/>
    public async Task<Abstractions.DialogResult> ShowYesNoCancelAsync(string message, string title)
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

    /// <inheritdoc/>
    public async Task ShowInformationAsync(string message, string title)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string message, string title)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
    }
}
