using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DocumentFileManager.Entities;
using DocumentFileManager.UI.Models;
using DocumentFileManager.ValueObjects;

namespace DocumentFileManager.UI.ViewModels;

/// <summary>
/// チェック項目の表示用ViewModel
/// MVVMパターンに準拠した設計
/// </summary>
public class CheckItemViewModel : INotifyPropertyChanged
{
    private bool _isChecked;
    private string? _captureFilePath;

    /// <summary>状態管理オブジェクト</summary>
    public CheckItemState State { get; private set; }

    /// <summary>チェック項目エンティティ</summary>
    public CheckItem Entity { get; }

    /// <summary>ID</summary>
    public int Id => Entity.Id;

    /// <summary>ラベル（表示名）</summary>
    public string Label => Entity.Label;

    /// <summary>パス</summary>
    public string Path => Entity.Path;

    /// <summary>チェック状態（UI用）</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();

                // ステータスを更新
                Entity.Status = value ? ItemStatus.Current : ItemStatus.Unspecified;
                OnPropertyChanged(nameof(Status));

                // CheckItemStateのItemStateも更新（チェック状態を反映）
                UpdateItemStateFromCheckState();

                // カメラボタンの表示状態も更新
                OnPropertyChanged(nameof(CameraButtonVisibility));
            }
        }
    }

    /// <summary>状態</summary>
    public ItemStatus Status => Entity.Status;

    /// <summary>キャプチャ画像のファイルパス（相対パス）</summary>
    public string? CaptureFilePath
    {
        get => _captureFilePath;
        set
        {
            if (_captureFilePath != value)
            {
                _captureFilePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCapture));

                // CheckItemStateのCaptureFileExistsも更新（ファイル存在チェック）
                UpdateCaptureFileExistsFromPath();

                OnPropertyChanged(nameof(CameraButtonVisibility));
            }
        }
    }

    /// <summary>キャプチャ画像が存在するかどうか</summary>
    public bool HasCapture => !string.IsNullOrEmpty(_captureFilePath);

    /// <summary>子要素のコレクション</summary>
    public ObservableCollection<CheckItemViewModel> Children { get; }

    /// <summary>分類かどうか（子要素を持つ）</summary>
    public bool IsCategory => Children.Count > 0;

    /// <summary>チェック項目かどうか（子要素を持たない）</summary>
    public bool IsItem => Children.Count == 0;

    #region 表示プロパティ

    /// <summary>ドキュメントルートパス</summary>
    public string DocumentRootPath { get; }

    /// <summary>MainWindowモードかどうか</summary>
    public bool IsMainWindow { get; }

    /// <summary>チェックボックスが有効かどうか（MainWindowモードでは無効）</summary>
    public bool IsCheckBoxEnabled => !IsMainWindow;

    /// <summary>カメラボタンの表示状態</summary>
    /// <remarks>
    /// MainWindowモード: キャプチャがあれば表示（IsCheckedに関係なく）
    /// ChecklistWindowモード: チェックON かつ キャプチャがある場合のみ表示
    /// 計算ロジックはCheckItemStateに委譲
    /// </remarks>
    public Visibility CameraButtonVisibility => State.CameraButtonVisibility;

    /// <summary>
    /// キャプチャの絶対パスを取得
    /// </summary>
    /// <returns>絶対パス（CaptureFilePathがnullの場合はnull）</returns>
    public string? GetCaptureAbsolutePath()
    {
        if (string.IsNullOrEmpty(CaptureFilePath))
            return null;

        return System.IO.Path.Combine(DocumentRootPath, CaptureFilePath);
    }

    #endregion

    #region コマンド

    private ICommand? _selectCommand;
    private ICommand? _checkedChangedCommand;
    private ICommand? _viewCaptureCommand;

    /// <summary>
    /// 選択コマンド（MainWindow用: クリックで資料フィルタリング）
    /// </summary>
    public ICommand? SelectCommand
    {
        get => _selectCommand;
        set
        {
            if (_selectCommand != value)
            {
                _selectCommand = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// チェック状態変更コマンド（ChecklistWindow用）
    /// </summary>
    public ICommand? CheckedChangedCommand
    {
        get => _checkedChangedCommand;
        set
        {
            if (_checkedChangedCommand != value)
            {
                _checkedChangedCommand = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// キャプチャ表示コマンド
    /// </summary>
    public ICommand? ViewCaptureCommand
    {
        get => _viewCaptureCommand;
        set
        {
            if (_viewCaptureCommand != value)
            {
                _viewCaptureCommand = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// キャプチャボタンの表示状態を更新する
    /// </summary>
    public void UpdateCaptureButton()
    {
        OnPropertyChanged(nameof(HasCapture));
        OnPropertyChanged(nameof(CameraButtonVisibility));
    }

    #endregion

    /// <summary>
    /// 既存のコンストラクタ（後方互換性のため維持）
    /// </summary>
    public CheckItemViewModel(CheckItem entity)
        : this(entity, string.Empty, false)
    {
    }

    /// <summary>
    /// 拡張コンストラクタ
    /// </summary>
    /// <param name="entity">チェック項目エンティティ</param>
    /// <param name="documentRootPath">ドキュメントルートパス</param>
    /// <param name="isMainWindow">MainWindowモードかどうか</param>
    public CheckItemViewModel(CheckItem entity, string documentRootPath, bool isMainWindow)
    {
        Entity = entity;
        DocumentRootPath = documentRootPath;
        IsMainWindow = isMainWindow;
        Children = new ObservableCollection<CheckItemViewModel>();

        // 初期状態をエンティティから設定
        _isChecked = entity.Status == ItemStatus.Current;

        // CheckItemStateを初期化（ファイル存在チェックはここで1回のみ実行）
        var windowMode = isMainWindow ? WindowMode.MainWindow : WindowMode.ChecklistWindow;
        var itemState = DetermineInitialItemState(entity.Status);
        State = new CheckItemState(windowMode, itemState, false); // CaptureFileExistsは後で設定
    }

    /// <summary>
    /// ItemStatusから初期ItemStateコードを決定
    /// </summary>
    private static string DetermineInitialItemState(ItemStatus status)
    {
        return status switch
        {
            ItemStatus.Current => "10",  // チェックON、キャプチャなし（初期）
            ItemStatus.Unspecified => "00", // 未紐づけ
            _ => "00"
        };
    }

    /// <summary>
    /// ItemState（状態コード）を更新
    /// </summary>
    public void UpdateItemState(string newItemState)
    {
        if (State.ItemState != newItemState)
        {
            State.ItemState = newItemState;
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(CameraButtonVisibility));
        }
    }

    /// <summary>
    /// CaptureFileExistsを更新
    /// </summary>
    public void UpdateCaptureFileExists(bool exists)
    {
        if (State.CaptureFileExists != exists)
        {
            State.CaptureFileExists = exists;
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(CameraButtonVisibility));
        }
    }

    /// <summary>
    /// CaptureFilePathからCaptureFileExistsとItemStateを更新
    /// </summary>
    private void UpdateCaptureFileExistsFromPath()
    {
        bool fileExists;
        if (string.IsNullOrEmpty(_captureFilePath) || string.IsNullOrEmpty(DocumentRootPath))
        {
            fileExists = false;
        }
        else
        {
            var absolutePath = System.IO.Path.Combine(DocumentRootPath, _captureFilePath);
            fileExists = File.Exists(absolutePath);
        }

        UpdateCaptureFileExists(fileExists);

        // ItemStateのキャプチャ状態部分も更新
        UpdateItemStateCaptureFlag(fileExists);
    }

    /// <summary>
    /// ItemStateのキャプチャ状態部分（2文字目）を更新
    /// </summary>
    private void UpdateItemStateCaptureFlag(bool hasCapture)
    {
        var currentCheckState = State.ItemState?.Length >= 1 ? State.ItemState[0] : '0';

        if (currentCheckState == '1')
        {
            // チェックON状態: 10 or 11
            UpdateItemState(hasCapture ? "11" : "10");
        }
        else if (currentCheckState == '2')
        {
            // チェックOFF（履歴あり）: 20 or 22
            UpdateItemState(hasCapture ? "22" : "20");
        }
        // currentCheckState == '0' の場合は未紐づけなので変更しない
    }

    /// <summary>
    /// チェック状態からItemStateを更新
    /// </summary>
    private void UpdateItemStateFromCheckState()
    {
        // CaptureFileExistsの状態を使用してItemStateを決定
        var hasCapture = State.CaptureFileExists;

        if (_isChecked)
        {
            // チェックON: 10 or 11
            UpdateItemState(hasCapture ? "11" : "10");
        }
        else
        {
            // チェックOFF: 20 or 22
            UpdateItemState(hasCapture ? "22" : "20");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
