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
    private string Field => FieldCombo.Text.Trim();

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
        ShowSpecialties();

        existingCvs = store.ListCvs(profile).Where(cv => !cv.Unreadable).ToList();
        ExistingCombo.ItemsSource = existingCvs.Select(cv => $"{cv.Company} - {cv.Role}").ToList();
        ExistingCombo.SelectedIndex = 0;
        ExistingOption.IsEnabled = existingCvs.Count > 0;

        ShowPictureNote();
        Loaded += (s, e) => CompanyBox.Focus();
    }

    private void ShowPictureNote() =>
        NoPictureNote.Visibility = store.HasPicture(profile) ? Visibility.Collapsed : Visibility.Visible;

    // No picture: ask one
    private void PictureOption_Click(object sender, RoutedEventArgs e)
    {
        if (PictureOption.IsChecked == true && !store.HasPicture(profile) && !App.ChoosePicture(this, store, profile))
            PictureOption.IsChecked = false;
        ShowPictureNote();
    }

    private void FieldCombo_Changed(object sender, RoutedEventArgs e) => ShowSpecialties();

    // List the Field's Specialties
    private void ShowSpecialties()
    {
        var specialties = Field == "" ? new List<string>() : store.ListSpecialties(profile, Field);
        SpecialtyCombo.ItemsSource = specialties;
        SpecialtyCombo.SelectedIndex = 0;

        SpecialtyOption.IsEnabled = specialties.Count > 0;
        SpecialtyOption.ToolTip = specialties.Count > 0 ? null : Text.Get("This Field has no Specialties yet");
        if (specialties.Count == 0 && SpecialtyOption.IsChecked == true)
            ScratchOption.IsChecked = true;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (Company == "" || Role == "" || Field == "")
        {
            MessageBox.Show(this, Text.Get("Please enter the company, the role and the Field."), "Fast Vitae");
            return;
        }

        if (store.CvExists(profile, Company, Role))
        {
            MessageBox.Show(this, Text.Get("You already have a CV for this company and role. It will be opened."), "Fast Vitae");
            Document = store.LoadCv(profile, Company, Role);
            AlreadyExists = true;
            DialogResult = true;
            return;
        }

        ShowSpecialties();
        if (SpecialtyOption.IsChecked == true && SpecialtyCombo.SelectedItem is string specialty)
        {
            // Specialty content, profile contact
            Document = store.LoadSpecialty(profile, Field, specialty);
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

        // A typed Field is created
        store.CreateField(profile, Field);
        Document.Field = NameCleaner.Clean(Field);
        Document.ShowPicture = PictureOption.IsChecked == true;
        DialogResult = true;
    }
}
