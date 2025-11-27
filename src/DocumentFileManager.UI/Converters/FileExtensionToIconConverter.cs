using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DocumentFileManager.UI.Helpers;

namespace DocumentFileManager.UI.Converters;

/// <summary>
/// ファイル拡張子をアイコンImageSourceに変換するコンバーター
/// </summary>
public class FileExtensionToIconConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string extension && !string.IsNullOrEmpty(extension))
        {
            // 拡張子が"."で始まっていない場合は追加
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            return FileIconHelper.GetIconByExtension(extension);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
