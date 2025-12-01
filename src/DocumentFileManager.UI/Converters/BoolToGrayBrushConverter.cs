using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DocumentFileManager.UI.Converters;

/// <summary>
/// bool値をBrushに変換するコンバーター
/// IsLatest=falseの場合にグレーを返す
/// </summary>
public class BoolToGrayBrushConverter : IValueConverter
{
    /// <summary>通常時の色（デフォルト: 黒）</summary>
    public Brush NormalBrush { get; set; } = Brushes.Black;

    /// <summary>グレー時の色（デフォルト: Gray）</summary>
    public Brush GrayBrush { get; set; } = Brushes.Gray;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isLatest)
        {
            return isLatest ? NormalBrush : GrayBrush;
        }
        return NormalBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// bool値を背景Brushに変換するコンバーター
/// IsLatest=falseの場合に薄いグレー背景を返す
/// </summary>
public class BoolToBackgroundBrushConverter : IValueConverter
{
    /// <summary>通常時の背景色（デフォルト: 透明）</summary>
    public Brush NormalBrush { get; set; } = Brushes.Transparent;

    /// <summary>グレー時の背景色（デフォルト: 薄いグレー）</summary>
    public Brush GrayBrush { get; set; } = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #F5F5F5

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isLatest)
        {
            return isLatest ? NormalBrush : GrayBrush;
        }
        return NormalBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
