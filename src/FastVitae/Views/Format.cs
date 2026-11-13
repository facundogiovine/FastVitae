using System.Windows;
using System.Windows.Controls;

namespace FastVitae.Views;

// Right-click opens text format
public static class Format
{
    // Which CV field the box fills
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(Format), new PropertyMetadata("", OnKeySet));

    public static string GetKey(DependencyObject box) => (string)box.GetValue(KeyProperty);
    public static void SetKey(DependencyObject box, string value) => box.SetValue(KeyProperty, value);

    private static void OnKeySet(DependencyObject box, DependencyPropertyChangedEventArgs e)
    {
        var control = (Control)box;
        control.ContextMenuOpening += (s, args) =>
        {
            // Selected text: normal edit menu
            if (control is TextBox { SelectionLength: > 0 })
                return;
            args.Handled = true;
            if (Window.GetWindow(control) is EditorWindow editor)
                editor.EditStyle(control, GetKey(control));
        };

        // Context menu event needs one
        if (control is not TextBox)
            control.ContextMenu = new ContextMenu();
    }
}
