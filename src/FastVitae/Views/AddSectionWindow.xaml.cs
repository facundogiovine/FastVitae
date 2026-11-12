using System.Windows;
using FastVitae.Model;

namespace FastVitae.Views;

// New section title and format
public partial class AddSectionWindow : Window
{
    public Section? Section { get; private set; }

    public AddSectionWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => TitleBox.Focus();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (TitleBox.Text.Trim() == "")
        {
            MessageBox.Show(this, "Please enter a title.", "Fast Vitae");
            return;
        }

        Section = new Section
        {
            Title = TitleBox.Text.Trim(),
            Layout = ColumnsOption.IsChecked == true ? SectionLayout.BulletColumns : SectionLayout.Entries
        };
        DialogResult = true;
    }
}
