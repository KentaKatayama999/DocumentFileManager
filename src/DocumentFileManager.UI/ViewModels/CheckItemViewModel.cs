using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DocumentFileManager.Entities;
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
    /// </remarks>
    public Visibility CameraButtonVisibility
    {
        get
        {
            // キャプチャがなければ非表示
            if (!HasCapture)
                return Visibility.Collapsed;

            // ファイルが存在しなければ非表示
            var absolutePath = GetCaptureAbsolutePath();
            if (absolutePath == null || !File.Exists(absolutePath))
                return Visibility.Collapsed;

            // ChecklistWindowモード（編集可能）の場合、チェックON時のみ表示
            // MainWindowモード（読み取り専用）の場合、キャプチャがあれば常に表示
            if (!IsMainWindow && !IsChecked)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }
    }

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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
