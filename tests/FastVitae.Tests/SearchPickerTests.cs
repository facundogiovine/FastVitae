using System.Windows.Controls;
using FastVitae.Model;
using FastVitae.Views;
using Xunit;

namespace FastVitae.Tests;

public class SearchPickerTests
{
    private static void OnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null)
            throw new Xunit.Sdk.XunitException(error.ToString());
    }

    private static ComboBox Picker(string kind, string value)
    {
        var combo = new ComboBox();
        Search.SetKind(combo, kind);
        Search.SetValue(combo, value);
        return combo;
    }

    [Fact]
    public void Country_StartsOnValue() => OnSta(() =>
        Assert.Equal("PY", ((Country)Picker("Country", "PY").SelectedItem).Iso));

    [Fact]
    public void Country_PinnedFirst() => OnSta(() =>
        Assert.Equal("US", ((Country)Picker("Country", "PY").Items[0]).Iso));

    [Fact]
    public void Typing_FiltersButKeepsValue() => OnSta(() =>
    {
        var combo = Picker("Country", "PY");
        Search.Filter(combo, "pola");
        Assert.Equal("PL", ((Country)combo.Items[0]).Iso);
        Assert.Equal("PY", Search.GetValue(combo));
    });

    [Fact]
    public void Clearing_RestoresSelection() => OnSta(() =>
    {
        var combo = Picker("Country", "PY");
        Search.Filter(combo, "pola");
        Search.Filter(combo, "");
        Assert.Equal("PY", ((Country)combo.SelectedItem).Iso);
    });

    [Fact]
    public void Picking_SetsValue() => OnSta(() =>
    {
        var combo = Picker("Currency", "USD");
        Search.Filter(combo, "zloty");
        combo.SelectedIndex = 0;
        Assert.Equal("PLN", Search.GetValue(combo));
    });

    // Focus must stay in the box
    [Theory]
    [InlineData("pola")]
    [InlineData("zzz")]
    public void Filtering_KeepsKeyboardFocus(string typed) => OnSta(() =>
    {
        var combo = Picker("Country", "PY");
        var window = new System.Windows.Window { Content = combo, Left = -3000, Width = 300, Height = 400 };
        window.Show();
        window.Activate();
        combo.IsDropDownOpen = true;
        Idle(combo);
        var selected = (System.Windows.UIElement)combo.ItemContainerGenerator.ContainerFromItem(combo.SelectedItem);
        System.Windows.Input.Keyboard.Focus(selected);
        Assert.True(combo.IsKeyboardFocusWithin);

        Search.Filter(combo, typed);
        Idle(combo);

        Assert.True(combo.IsKeyboardFocusWithin);
        window.Close();
    });

    private static void Idle(ComboBox combo) =>
        combo.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

    [Fact]
    public void Currency_EmptyValue_IsDollar() => OnSta(() =>
        Assert.Equal("USD", ((Currency)Picker("Currency", "").SelectedItem).Code));
}
