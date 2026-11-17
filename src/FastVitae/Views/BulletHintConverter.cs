using System.Globalization;
using System.Windows.Data;

namespace FastVitae.Views;

// Position to "Bullet 1"
public class BulletHintConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        $"{Text.Get("Bullet")} {(value is int index ? index + 1 : 1)}";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
