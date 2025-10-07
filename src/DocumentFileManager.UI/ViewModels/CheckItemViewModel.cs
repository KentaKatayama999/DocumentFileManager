using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocumentFileManager.Entities;
using DocumentFileManager.ValueObjects;

namespace DocumentFileManager.UI.ViewModels;

/// <summary>
/// チェック項目の表示用ViewModel
/// </summary>
public class CheckItemViewModel : INotifyPropertyChanged
{
    private bool _isChecked;

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

    /// <summary>子要素のコレクション</summary>
    public ObservableCollection<CheckItemViewModel> Children { get; }

    /// <summary>分類かどうか（子要素を持つ）</summary>
    public bool IsCategory => Children.Count > 0;

    /// <summary>チェック項目かどうか（子要素を持たない）</summary>
    public bool IsItem => Children.Count == 0;

    public CheckItemViewModel(CheckItem entity)
    {
        Entity = entity;
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
