using System.Windows;
using System.Windows.Controls;
using FastVitae.Model;
using FastVitae.Storage;

namespace FastVitae.Views;

// Company, role and starting point
public partial class NewCvWindow : Window
{
    private readonly ProfileStore store;
    private readonly string profile;
    private readonly List<CvListItem> existingCvs;

    public string Company => CompanyBox.Text.Trim();
    public string Role => RoleBox.Text.Trim();

    // Content for the editor
    public CvDocument? Document { get; private set; }

    // CV was already on disk
    public bool AlreadyExists { get; private set; }

    public NewCvWindow(ProfileStore store, string profile)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;

        FieldCombo.ItemsSource = store.ListFields(profile);
        FieldCombo.SelectedIndex = 0;

        existingCvs = store.ListCvs(profile).Where(cv => !cv.Unreadable).ToList();
        ExistingCombo.ItemsSource = existingCvs.Select(cv => $"{cv.Company} - {cv.Role}").ToList();
        ExistingCombo.SelectedIndex = 0;

        // Disable empty options
        SpecialtyOption.IsEnabled = FieldCombo.Items.Count > 0;
        ExistingOption.IsEnabled = existingCvs.Count > 0;
        if (!store.HasPicture(profile))
        {
            PictureOption.IsEnabled = false;
            PictureOption.ToolTip = "Add a picture in Profile first";
            ToolTipService.SetShowOnDisabled(PictureOption, true);
        }

        Loaded += (s, e) => CompanyBox.Focus();
    }

    private void FieldCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FieldCombo.SelectedItem is string field)
        {
            SpecialtyCombo.ItemsSource = store.ListSpecialties(profile, field);
            SpecialtyCombo.SelectedIndex = 0;
        }
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (Company == "" || Role == "")
        {
            MessageBox.Show(this, "Please enter the company and the role.", "Fast Vitae");
            return;
        }

        if (store.CvExists(profile, Company, Role))
        {
            MessageBox.Show(this, "You already have a CV for this company and role. It will be opened.", "Fast Vitae");
            Document = store.LoadCv(profile, Company, Role);
            AlreadyExists = true;
        }
        else if (SpecialtyOption.IsChecked == true)
        {
            if (FieldCombo.SelectedItem is not string field || SpecialtyCombo.SelectedItem is not string specialty)
            {
                MessageBox.Show(this, "Please pick a Field and a Specialty.", "Fast Vitae");
                return;
            }
            // Specialty content, profile contact
            Document = store.LoadSpecialty(profile, field, specialty);
            Document.BasicInfo = store.LoadProfile(profile);
        }
        else if (ExistingOption.IsChecked == true)
        {
            var source = existingCvs[ExistingCombo.SelectedIndex];
            Document = store.LoadCv(profile, source.Company, source.Role);
        }
        else
        {
            Document = CvDocument.NewFromScratch(store.LoadProfile(profile));
        }

        // Existing CV keeps its setting
        if (!AlreadyExists)
            Document.ShowPicture = PictureOption.IsChecked == true;

        DialogResult = true;
    }
}
