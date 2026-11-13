using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FastVitae.Views;

// Gray text in empty boxes
public static class Hint
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(Hint), new PropertyMetadata("", OnTextSet));

    public static string GetText(DependencyObject box) => (string)box.GetValue(TextProperty);
    public static void SetText(DependencyObject box, string value) => box.SetValue(TextProperty, value);

    private static void OnTextSet(DependencyObject box, DependencyPropertyChangedEventArgs e)
    {
        var control = (Control)box;

        // Language switch: redraw only
        if ((string)e.OldValue != "")
        {
            Refresh(control);
            return;
        }

        control.Loaded += (s, _) => Refresh(control);
        control.Unloaded += (s, _) => RemoveHint(control);
        control.IsVisibleChanged += (s, _) => Refresh(control);
        if (control is TextBox textBox)
            textBox.TextChanged += (s, _) => Refresh(control);
        if (control is ComboBox comboBox)
            comboBox.SelectionChanged += (s, _) => Refresh(control);
    }

    private static void Refresh(Control control)
    {
        bool empty = control switch
        {
            TextBox textBox => textBox.Text == "",
            ComboBox comboBox => string.IsNullOrEmpty(comboBox.SelectedItem as string),
            _ => false
        };

        RemoveHint(control);
        var layer = AdornerLayer.GetAdornerLayer(control);
        if (empty && control.IsVisible && layer != null)
            layer.Add(new HintAdorner(control));
    }

    private static void RemoveHint(Control control)
    {
        var layer = AdornerLayer.GetAdornerLayer(control);
        foreach (var hint in layer?.GetAdorners(control)?.OfType<HintAdorner>() ?? [])
            layer!.Remove(hint);
    }

    // Draws the gray text
    private class HintAdorner : Adorner
    {
        private static readonly Brush Gray = new SolidColorBrush(Color.FromRgb(0x9A, 0x9C, 0xA0));

        public HintAdorner(Control control) : base(control)
        {
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawing)
        {
            var control = (Control)AdornedElement;
            var text = new FormattedText(GetText(control), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(control.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                control.FontSize, Gray, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double left = control.BorderThickness.Left + control.Padding.Left + 2;
            // Text boxes start at top
            double top = control is TextBox
                ? control.BorderThickness.Top + control.Padding.Top + 1
                : (control.ActualHeight - text.Height) / 2;
            drawing.DrawText(text, new Point(left, top));
        }
    }
}
