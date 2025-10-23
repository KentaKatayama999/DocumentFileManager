using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DocumentFileManager.UI.ViewModels;

/// <summary>
/// チェック項目エディター用ViewModel
/// </summary>
public class CheckItemEditorViewModel : INotifyPropertyChanged
{
    private string _label = string.Empty;
    private string _type = "category";
    private bool _checked = false;

    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            OnPropertyChanged(nameof(Label));
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            _type = value;
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(TypeIcon));
            OnPropertyChanged(nameof(TypeLabel));
        }
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            OnPropertyChanged(nameof(Checked));
        }
    }

    public ObservableCollection<CheckItemEditorViewModel> Children { get; set; } = new();

    public string TypeIcon => Type == "category" ? "📁" : "📄";
    public string TypeLabel => Type == "category" ? "[カテゴリ]" : "[項目]";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
