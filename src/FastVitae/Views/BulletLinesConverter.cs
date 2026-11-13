using System.Globalization;
using System.Windows.Data;

namespace FastVitae.Views;

// Bullet list as text lines
public class BulletLinesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Join(Environment.NewLine, (List<string>)value);
    }

    // Keep the text as typed
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((string)value)
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToList();
    }
}
