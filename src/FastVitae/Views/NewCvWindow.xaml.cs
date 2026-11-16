using System.Windows;
using FastVitae.Model;
using FastVitae.Storage;

namespace FastVitae.Views;

// Field, role and starting point
public partial class NewCvWindow : Window
{
    private readonly ProfileStore store;
    private readonly string profile;
    private readonly List<CvListItem> existingCvs;

    public string Field => NameCleaner.Clean(FieldCombo.Text.Trim());
    public string Role => RoleBox.Text.Trim();
    private string TypedField => FieldCombo.Text.Trim();
    private string CvLanguage => LanguageBox.SelectedValue as string ?? "en";

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
        ExistingCombo.ItemsSource = existingCvs.Select(cv => $"{cv.Field} - {cv.Role}").ToList();
        ExistingCombo.SelectedIndex = 0;
        ExistingOption.IsEnabled = existingCvs.Count > 0;

        ShowPictureNote();
        Loaded += (s, e) => FieldCombo.Focus();
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
        var specialties = TypedField == "" ? new List<string>() : store.ListSpecialties(profile, TypedField);
        SpecialtyCombo.ItemsSource = specialties;
        SpecialtyCombo.SelectedIndex = 0;

        SpecialtyOption.IsEnabled = specialties.Count > 0;
        SpecialtyOption.ToolTip = specialties.Count > 0 ? null : Text.Get("This Field has no Specialties yet");
        if (specialties.Count == 0 && SpecialtyOption.IsChecked == true)
            ScratchOption.IsChecked = true;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (Role == "" || TypedField == "")
        {
            MessageBox.Show(this, Text.Get("Please enter the Field and the role."), "Fast Vitae");
            return;
        }

        if (store.CvExists(profile, Field, Role))
        {
            MessageBox.Show(this, Text.Get("You already have a CV for this Field and role. It will be opened."), "Fast Vitae");
            Document = store.LoadCv(profile, Field, Role);
            AlreadyExists = true;
            DialogResult = true;
            return;
        }

        ShowSpecialties();
        if (SpecialtyOption.IsChecked == true && SpecialtyCombo.SelectedItem is string specialty)
        {
            // Specialty content, profile contact
            Document = store.LoadSpecialty(profile, TypedField, specialty);
            Document.BasicInfo = store.LoadProfile(profile);
            Document.StartAsNewCv(CvLanguage);
        }
        else if (ExistingOption.IsChecked == true)
        {
            var source = existingCvs[ExistingCombo.SelectedIndex];
            Document = store.LoadCv(profile, source.Field, source.Role);
            Document.StartAsNewCv(CvLanguage);
        }
        else
        {
            Document = CvDocument.NewFromScratch(store.LoadProfile(profile), CvLanguage);
        }

        // A typed Field is created
        store.CreateField(profile, TypedField);
        Document.Field = Field;
        Document.ShowPicture = PictureOption.IsChecked == true;
        DialogResult = true;
    }
}
