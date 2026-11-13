using System.Windows;
using System.Windows.Controls;
using FastVitae.Storage;

namespace FastVitae.Views;

// Pick Field and Specialty
public partial class SaveToSpecialtyWindow : Window
{
    private readonly ProfileStore store;
    private readonly string profile;

    public string Field => FieldCombo.Text.Trim();
    public string Specialty => SpecialtyCombo.Text.Trim();

    public SaveToSpecialtyWindow(ProfileStore store, string profile)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;
        FieldCombo.ItemsSource = store.ListFields(profile);
        Loaded += (s, e) => FieldCombo.Focus();
    }

    private void FieldCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FieldCombo.SelectedItem is string field)
            SpecialtyCombo.ItemsSource = store.ListSpecialties(profile, field);
    }

    // Typed Field fills Specialties
    private void FieldCombo_LostFocus(object sender, RoutedEventArgs e)
    {
        if (Field != "")
            SpecialtyCombo.ItemsSource = store.ListSpecialties(profile, Field);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Field == "" || Specialty == "")
        {
            MessageBox.Show(this, Text.Get("Please enter a Field and a Specialty."), "Fast Vitae");
            return;
        }

        if (store.SpecialtyExists(profile, Field, Specialty))
        {
            var answer = MessageBox.Show(this,
                Text.Format("{0} - {1} already has saved content. Replace it?", Field, Specialty),
                "Fast Vitae", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
                return;
        }
        DialogResult = true;
    }
}
