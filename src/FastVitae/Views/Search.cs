using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using FastVitae.Model;

namespace FastVitae.Views;

// Type to filter a dropdown
public static class Search
{
    // "Country" or "Currency"
    public static readonly DependencyProperty KindProperty = DependencyProperty.RegisterAttached(
        "Kind", typeof(string), typeof(Search), new PropertyMetadata("", OnKindSet));

    public static string GetKind(DependencyObject combo) => (string)combo.GetValue(KindProperty);
    public static void SetKind(DependencyObject combo, string value) => combo.SetValue(KindProperty, value);

    // Iso or currency code
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        "Value", typeof(string), typeof(Search),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueSet));

    public static string GetValue(DependencyObject combo) => (string)combo.GetValue(ValueProperty);
    public static void SetValue(DependencyObject combo, string value) => combo.SetValue(ValueProperty, value);

    // Typed text, per box
    private static readonly DependencyProperty TypedProperty = DependencyProperty.RegisterAttached(
        "Typed", typeof(string), typeof(Search), new PropertyMetadata(""));

    // Ignore our own selections
    private static readonly DependencyProperty BusyProperty = DependencyProperty.RegisterAttached(
        "Busy", typeof(bool), typeof(Search), new PropertyMetadata(false));

    // Shows what was typed
    private static readonly DependencyProperty TypedPopupProperty = DependencyProperty.RegisterAttached(
        "TypedPopup", typeof(Popup), typeof(Search), new PropertyMetadata(null));

    private static void OnKindSet(DependencyObject box, DependencyPropertyChangedEventArgs e)
    {
        var combo = (ComboBox)box;
        bool countries = (string)e.NewValue == "Country";
        System.Collections.IList items = countries ? Country.Ordered : Currency.All.ToList();
        combo.SelectedValuePath = countries ? nameof(Country.Iso) : nameof(Currency.Code);
        combo.IsTextSearchEnabled = false;
        // Else loading picks the first
        combo.IsSynchronizedWithCurrentItem = false;
        Quietly(combo, () => combo.ItemsSource = new ListCollectionView(items));
        SelectValue(combo);

        combo.PreviewTextInput += (s, args) =>
        {
            combo.IsDropDownOpen = true;
            Filter(combo, (string)combo.GetValue(TypedProperty) + args.Text);
            args.Handled = true;
        };
        combo.PreviewKeyDown += (s, args) =>
        {
            string typed = (string)combo.GetValue(TypedProperty);
            if (args.Key == Key.Back && typed != "")
            {
                Filter(combo, typed[..^1]);
                args.Handled = true;
            }
        };
        combo.SelectionChanged += (s, args) =>
        {
            if (!(bool)combo.GetValue(BusyProperty) && combo.SelectedValue is string picked)
                SetValue(combo, picked);
        };
        combo.DropDownClosed += (s, args) => Filter(combo, "");
    }

    private static void OnValueSet(DependencyObject box, DependencyPropertyChangedEventArgs e)
    {
        if (box is ComboBox combo && combo.ItemsSource != null)
            SelectValue(combo);
    }

    // Filter the list; value stays
    public static void Filter(ComboBox combo, string typed)
    {
        combo.SetValue(TypedProperty, typed);
        bool countries = GetKind(combo) == "Country";
        Quietly(combo, () =>
        {
            var view = (ListCollectionView)combo.ItemsSource;
            view.Filter = typed == "" ? null
                : item => countries ? Country.Matches((Country)item, typed) : Currency.Matches((Currency)item, typed);
            if (typed != "")
                combo.SelectedIndex = -1;
        });
        if (typed == "")
            SelectValue(combo);
        ShowTyped(combo, typed);

        // Keep keys in the list
        combo.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () => FocusFirst(combo));
    }

    private static void FocusFirst(ComboBox combo)
    {
        if (!combo.IsDropDownOpen)
            return;
        combo.UpdateLayout();
        bool focused = combo.ItemContainerGenerator.ContainerFromIndex(0) is ComboBoxItem first && first.Focus();
        if (!focused)
            combo.Focus();
    }

    // Select the saved value
    private static void SelectValue(ComboBox combo)
    {
        string value = GetValue(combo);
        if (value == "" && GetKind(combo) == "Currency")
            value = Currency.Default.Code;
        Quietly(combo, () => combo.SelectedValue = value);
    }

    private static void Quietly(ComboBox combo, Action action)
    {
        combo.SetValue(BusyProperty, true);
        try
        {
            action();
        }
        finally
        {
            combo.SetValue(BusyProperty, false);
        }
    }

    // Typed text over the box
    private static void ShowTyped(ComboBox combo, string typed)
    {
        if (combo.GetValue(TypedPopupProperty) is not Popup popup)
        {
            if (typed == "")
                return;
            popup = new Popup
            {
                PlacementTarget = combo,
                Placement = PlacementMode.Relative,
                AllowsTransparency = true,
                IsHitTestVisible = false,
                Child = new Border
                {
                    Background = (System.Windows.Media.Brush)combo.TryFindResource("InsetBrush") ?? System.Windows.Media.Brushes.DimGray,
                    BorderBrush = (System.Windows.Media.Brush)combo.TryFindResource("LineBrush") ?? System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 0, 10, 0),
                    Child = new TextBlock { Foreground = System.Windows.Media.Brushes.White, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis }
                }
            };
            combo.SetValue(TypedPopupProperty, popup);
        }
        var border = (Border)popup.Child;
        border.Width = combo.ActualWidth;
        border.Height = combo.ActualHeight;
        ((TextBlock)border.Child).Text = typed;
        popup.IsOpen = typed != "";
    }
}
