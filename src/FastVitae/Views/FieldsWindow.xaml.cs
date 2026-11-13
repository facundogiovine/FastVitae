using System.IO;
using System.Windows;
using System.Windows.Controls;
using FastVitae.Model;
using FastVitae.Storage;

namespace FastVitae.Views;

// Fields and their Specialties
public partial class FieldsWindow : Window
{
    private readonly ProfileStore store;
    private readonly string profile;

    private string? SelectedField => FieldList.SelectedItem as string;
    private string? SelectedSpecialty => SpecialtyList.SelectedItem as string;

    public FieldsWindow(ProfileStore store, string profile)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;
        ShowFields(select: null);
    }

    private void ShowFields(string? select)
    {
        var fields = store.ListFields(profile);
        FieldList.ItemsSource = fields;
        FieldList.SelectedItem = select != null && fields.Contains(select) ? select : fields.FirstOrDefault();
        ShowSpecialties(select: null);
    }

    private void ShowSpecialties(string? select)
    {
        var specialties = SelectedField == null ? new List<string>() : store.ListSpecialties(profile, SelectedField);
        SpecialtyList.ItemsSource = specialties;
        SpecialtyList.SelectedItem = select != null && specialties.Contains(select) ? select : specialties.FirstOrDefault();
    }

    private void FieldList_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowSpecialties(select: null);

    // ---- Fields ----

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        string? name = PromptWindow.Ask(this, Text.Get("Add Field"), Text.Get("Field name"));
        if (name == null)
            return;
        Try(() => store.CreateField(profile, name));
        ShowFields(NameCleaner.Clean(name));
    }

    private void RenameField_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedField == null)
            return;
        string? name = PromptWindow.Ask(this, Text.Get("Rename Field"), Text.Get("Field name"), SelectedField);
        if (name == null || NameCleaner.Clean(name) == SelectedField)
            return;
        Try(() => store.RenameField(profile, SelectedField, name));
        ShowFields(NameCleaner.Clean(name));
    }

    private void DeleteField_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedField == null || !Confirm(Text.Format("Delete the Field {0} and all its Specialties?", SelectedField)))
            return;
        Try(() => store.DeleteField(profile, SelectedField));
        ShowFields(select: null);
    }

    // ---- Specialties ----

    private void AddSpecialty_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedField == null)
        {
            MessageBox.Show(this, Text.Get("Add a Field first."), "Fast Vitae");
            return;
        }
        string? name = PromptWindow.Ask(this, Text.Get("Add Specialty"), Text.Get("Specialty name"));
        if (name == null)
            return;
        if (store.SpecialtyExists(profile, SelectedField, name))
        {
            MessageBox.Show(this, Text.Get("That Specialty already exists."), "Fast Vitae");
            return;
        }

        // Create empty, then open
        Try(() => store.SaveSpecialty(profile, SelectedField, name, CvDocument.NewFromScratch(new BasicInfo())));
        ShowSpecialties(NameCleaner.Clean(name));
        EditSpecialty_Click(sender, e);
    }

    private void EditSpecialty_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedField == null || SelectedSpecialty == null)
            return;

        CvDocument doc;
        try
        {
            doc = store.LoadSpecialty(profile, SelectedField, SelectedSpecialty);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            MessageBox.Show(this, Text.Get("This Specialty could not be read, so it was left untouched:") + " " + ex.Message, "Fast Vitae");
            return;
        }

        // Preview uses profile contact
        doc.BasicInfo = store.LoadProfile(profile);
        var editor = EditorWindow.ForSpecialty(store, profile, doc, SelectedField, SelectedSpecialty);
        editor.Owner = this;
        editor.ShowDialog();
    }

    private void RenameSpecialty_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedField == null || SelectedSpecialty == null)
            return;
        string? name = PromptWindow.Ask(this, Text.Get("Rename Specialty"), Text.Get("Specialty name"), SelectedSpecialty);
        if (name == null || NameCleaner.Clean(name) == SelectedSpecialty)
            return;
        Try(() => store.RenameSpecialty(profile, SelectedField, SelectedSpecialty, name));
        ShowSpecialties(NameCleaner.Clean(name));
    }

    private void DeleteSpecialty_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedField == null || SelectedSpecialty == null || !Confirm(Text.Format("Delete the Specialty {0}?", SelectedSpecialty)))
            return;
        Try(() => store.DeleteSpecialty(profile, SelectedField, SelectedSpecialty));
        ShowSpecialties(select: null);
    }

    // ---- Helpers ----

    private bool Confirm(string question)
    {
        return MessageBox.Show(this, question, "Fast Vitae",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    // Show file errors as messages
    private void Try(Action change)
    {
        try
        {
            change();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, Text.Get("That could not be done:") + " " + ex.Message, "Fast Vitae");
        }
    }
}
