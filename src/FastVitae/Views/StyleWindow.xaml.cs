using System.Windows;
using System.Windows.Media;
using FastVitae.Model;

namespace FastVitae.Views;

// Font, size, colour for one field
public partial class StyleWindow : Window
{
    private readonly string key;
    private bool filling;

    // Null after Reset: back to default
    public TextStyle? Result { get; private set; }

    public StyleWindow(string key, TextStyle start, string sample)
    {
        InitializeComponent();
        this.key = key;
        Sample.Text = sample.Trim() == "" ? "Aa Bb Cc 123" : sample.Trim().Split('\n')[0];
        ShowStyle(start);

        // Same kind of field, whole CV
        string? label = key switch
        {
            "Dates" => "Apply to all dates",
            "RoleTitle" => "Apply to all titles",
            "EntryName" => "Apply to all companies",
            "Place" => "Apply to all locations",
            _ => null
        };
        if (label != null)
        {
            ApplyAllButton.Content = Text.Get(label);
            ApplyAllButton.Visibility = Visibility.Visible;
        }
    }

    // Set when the style goes everywhere
    public bool ApplyToAll { get; private set; }

    private void ApplyAll_Click(object sender, RoutedEventArgs e)
    {
        ApplyToAll = true;
        Ok_Click(sender, e);
    }

    private void ShowStyle(TextStyle style)
    {
        filling = true;
        FontBox.SelectedItem = TextStyle.Fonts.Contains(style.Font) ? style.Font : TextStyle.DefaultFont;
        SizeBox.SelectedItem = style.Size;
        ColorBox.SelectedItem = TextStyle.Colors.Contains(style.Color) ? style.Color : TextStyle.Colors[0];
        BoldBox.IsChecked = style.Bold;
        ItalicBox.IsChecked = style.Italic;
        StrikeBox.IsChecked = style.Strike;
        filling = false;
        UpdateSample();
    }

    private TextStyle Picked() => new()
    {
        Font = FontBox.SelectedItem as string ?? TextStyle.DefaultFont,
        Size = SizeBox.SelectedItem as int? ?? 11,
        Color = ColorBox.SelectedItem as string ?? TextStyle.Colors[0],
        Bold = BoldBox.IsChecked == true,
        Italic = ItalicBox.IsChecked == true,
        Strike = StrikeBox.IsChecked == true
    };

    private void Changed(object sender, RoutedEventArgs e)
    {
        if (!filling)
            UpdateSample();
    }

    private void UpdateSample()
    {
        var style = Picked();
        Sample.FontFamily = new FontFamily(style.Font);
        Sample.FontSize = style.Size * 96.0 / 72.0;
        Sample.Foreground = (Brush)new BrushConverter().ConvertFromString(style.Color)!;
        Sample.FontWeight = style.Bold ? FontWeights.Bold : FontWeights.Normal;
        Sample.FontStyle = style.Italic ? FontStyles.Italic : FontStyles.Normal;
        Sample.TextDecorations = style.Strike ? TextDecorations.Strikethrough : null;
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => ShowStyle(TextStyle.DefaultFor(key));

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var picked = Picked();
        bool isDefault = System.Text.Json.JsonSerializer.Serialize(picked) == System.Text.Json.JsonSerializer.Serialize(TextStyle.DefaultFor(key));
        Result = isDefault ? null : picked;
        DialogResult = true;
    }
}
