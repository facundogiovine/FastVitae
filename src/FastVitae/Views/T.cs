using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace FastVitae.Views;

// XAML text in current language
public class T : MarkupExtension
{
    public string Key { get; set; }

    public T(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding(nameof(LiveText.Value)) { Source = LiveText.For(Key), Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}

// One key, updates on switch
public class LiveText : INotifyPropertyChanged
{
    private static readonly Dictionary<string, LiveText> cache = new();
    private readonly string key;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Value => Text.Get(key);

    private LiveText(string key)
    {
        this.key = key;
        Text.LanguageChanged += () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    public static LiveText For(string key)
    {
        if (!cache.TryGetValue(key, out var live))
            cache[key] = live = new LiveText(key);
        return live;
    }
}

// Stored English word, translated
public class TranslateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string word ? Text.Get(word) : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
