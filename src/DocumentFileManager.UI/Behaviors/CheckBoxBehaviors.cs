using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentFileManager.UI.Behaviors;

/// <summary>
/// CheckBox用のAttached Behaviors
/// Checked/Uncheckedイベントでコマンドを実行するためのヘルパー
/// </summary>
public static class CheckBoxBehaviors
{
    #region CheckedChangedCommand Attached Property

    /// <summary>
    /// CheckedChangedCommandプロパティ（ChecklistWindow用）
    /// CheckBoxのChecked/Uncheckedイベントで実行されるコマンド
    /// </summary>
    public static readonly DependencyProperty CheckedChangedCommandProperty =
        DependencyProperty.RegisterAttached(
            "CheckedChangedCommand",
            typeof(ICommand),
            typeof(CheckBoxBehaviors),
            new PropertyMetadata(null, OnCheckedChangedCommandChanged));

    public static ICommand GetCheckedChangedCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(CheckedChangedCommandProperty);
    }

    public static void SetCheckedChangedCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(CheckedChangedCommandProperty, value);
    }

    private static void OnCheckedChangedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CheckBox checkBox)
        {
            // 既存のハンドラを削除
            checkBox.Checked -= OnCheckBoxCheckedChanged;
            checkBox.Unchecked -= OnCheckBoxCheckedChanged;
            checkBox.Unloaded -= OnCheckedChangedCommandUnloaded;

            if (e.NewValue is ICommand)
            {
                // 新しいハンドラを追加
                checkBox.Checked += OnCheckBoxCheckedChanged;
                checkBox.Unchecked += OnCheckBoxCheckedChanged;
                checkBox.Unloaded += OnCheckedChangedCommandUnloaded;
            }
        }
    }

    private static void OnCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            var command = GetCheckedChangedCommand(checkBox);
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }

    private static void OnCheckedChangedCommandUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            // メモリリーク防止: イベントハンドラを明示的に解除
            checkBox.Checked -= OnCheckBoxCheckedChanged;
            checkBox.Unchecked -= OnCheckBoxCheckedChanged;
            checkBox.Unloaded -= OnCheckedChangedCommandUnloaded;
        }
    }

    #endregion

    #region ClickCommand Attached Property (MainWindow用: クリックでチェック状態を変えない)

    /// <summary>
    /// ClickCommandプロパティ（MainWindow用）
    /// クリック時にチェック状態を変更せずコマンドのみ実行
    /// PreviewMouseLeftButtonDownを使用してクリックをインターセプト
    /// </summary>
    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "ClickCommand",
            typeof(ICommand),
            typeof(CheckBoxBehaviors),
            new PropertyMetadata(null, OnClickCommandChanged));

    public static ICommand GetClickCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(ClickCommandProperty);
    }

    public static void SetClickCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(ClickCommandProperty, value);
    }

    private static void OnClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CheckBox checkBox)
        {
            // 既存のハンドラを削除
            checkBox.PreviewMouseLeftButtonDown -= OnCheckBoxPreviewMouseDown;
            checkBox.PreviewKeyDown -= OnCheckBoxPreviewKeyDown;
            checkBox.Unloaded -= OnClickCommandUnloaded;

            if (e.NewValue is ICommand)
            {
                // 新しいハンドラを追加
                // PreviewMouseLeftButtonDownでクリックをインターセプトし、チェック状態変更を防ぐ
                checkBox.PreviewMouseLeftButtonDown += OnCheckBoxPreviewMouseDown;
                checkBox.PreviewKeyDown += OnCheckBoxPreviewKeyDown;
                checkBox.Unloaded += OnClickCommandUnloaded;
            }
        }
    }

    private static void OnCheckBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            // デフォルトの動作（チェック状態変更）をキャンセル
            e.Handled = true;

            var command = GetClickCommand(checkBox);
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }

    private static void OnCheckBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // スペースキーでのチェック状態変更も防ぐ
        if (sender is CheckBox checkBox && e.Key == Key.Space)
        {
            e.Handled = true;

            var command = GetClickCommand(checkBox);
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }

    private static void OnClickCommandUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            // メモリリーク防止: イベントハンドラを明示的に解除
            checkBox.PreviewMouseLeftButtonDown -= OnCheckBoxPreviewMouseDown;
            checkBox.PreviewKeyDown -= OnCheckBoxPreviewKeyDown;
            checkBox.Unloaded -= OnClickCommandUnloaded;
        }
    }

    #endregion
}
