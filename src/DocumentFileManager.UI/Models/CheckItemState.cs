using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DocumentFileManager.UI.Models;

/// <summary>
/// ウィンドウモード（表示コンテキスト）
/// </summary>
public enum WindowMode
{
    /// <summary>メインウィンドウ（読み取り専用モード）</summary>
    MainWindow = 0,

    /// <summary>チェックリストウィンドウ（編集モード）</summary>
    ChecklistWindow = 1
}

/// <summary>
/// チェック項目の状態を管理するクラス
/// UI表示に必要な派生プロパティの計算を担当
/// </summary>
/// <remarks>
/// 状態パラメータ:
/// - WindowMode: ウィンドウ種別（MainWindow/ChecklistWindow）
/// - ItemState: チェック×キャプチャ状態（00/10/11/20/22）
/// - CaptureFileExists: キャプチャファイルの実在確認
///
/// ItemState状態コード:
/// - 00: 未紐づけ
/// - 10: チェックON、キャプチャなし
/// - 11: チェックON、キャプチャあり
/// - 20: チェックOFF（履歴あり）、キャプチャなし
/// - 22: チェックOFF（履歴あり）、キャプチャあり
/// </remarks>
public class CheckItemState : INotifyPropertyChanged
{
    private string _itemState;
    private bool _captureFileExists;

    /// <summary>
    /// ウィンドウモード
    /// </summary>
    public WindowMode WindowMode { get; }

    /// <summary>
    /// アイテム状態コード（00/10/11/20/22）
    /// </summary>
    public string ItemState
    {
        get => _itemState;
        set
        {
            if (_itemState != value)
            {
                _itemState = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// キャプチャファイルが存在するかどうか
    /// </summary>
    public bool CaptureFileExists
    {
        get => _captureFileExists;
        set
        {
            if (_captureFileExists != value)
            {
                _captureFileExists = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// CheckItemStateを初期化
    /// </summary>
    /// <param name="windowMode">ウィンドウモード</param>
    /// <param name="itemState">アイテム状態コード</param>
    /// <param name="captureFileExists">キャプチャファイルが存在するか</param>
    public CheckItemState(WindowMode windowMode, string itemState, bool captureFileExists)
    {
        WindowMode = windowMode;
        _itemState = itemState;
        _captureFileExists = captureFileExists;
    }

    /// <summary>
    /// チェックボックスが有効かどうか
    /// </summary>
    /// <remarks>
    /// MainWindow: 無効（読み取り専用）
    /// ChecklistWindow: 有効（編集可能）
    /// </remarks>
    public bool IsCheckBoxEnabled => WindowMode == WindowMode.ChecklistWindow;

    /// <summary>
    /// PropertyChanged イベント
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// PropertyChanged イベントを発火
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
