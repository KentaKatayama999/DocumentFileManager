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

    #region Phase 3拡張プロパティ

    /// <summary>ドキュメントルートパス</summary>
    public string DocumentRootPath { get; }

    /// <summary>MainWindowモードかどうか</summary>
    public bool IsMainWindow { get; }

    /// <summary>チェックボックスが有効かどうか（MainWindowモードでは無効）</summary>
    public bool IsCheckBoxEnabled => !IsMainWindow;

    /// <summary>カメラボタンの表示状態</summary>
    public Visibility CameraButtonVisibility
    {
        get
        {
            if (!HasCapture)
                return Visibility.Collapsed;

            var absolutePath = GetCaptureAbsolutePath();
            if (absolutePath == null || !File.Exists(absolutePath))
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

    #region Phase 4拡張プロパティ（ICommand）

    private ICommand? _checkedChangedCommand;
    private ICommand? _viewCaptureCommand;

    /// <summary>
    /// チェック状態変更コマンド
    /// 外部から設定される（CheckItemUIBuilderまたはChecklistWindow）
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
    /// 外部から設定される（CheckItemUIBuilderまたはChecklistWindow）
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
