using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FastVitae.Model;

namespace FastVitae.Views;

// Is this font on the PC
public class FontInstalledConverter : IValueConverter
{
    private static readonly HashSet<string> installed = Fonts.SystemFontFamilies
        .SelectMany(family => family.FamilyNames.Values)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // STIX ships with the app
    public static bool IsInstalled(string font) => font == TextStyle.DefaultFont || installed.Contains(font);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string font && IsInstalled(font);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
