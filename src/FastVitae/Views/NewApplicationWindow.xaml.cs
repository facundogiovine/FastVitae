using System.Windows;
using FastVitae.Storage;

namespace FastVitae.Views;

// Company, CV, salary and link
public partial class NewApplicationWindow : Window
{
    private readonly ProfileStore store;
    private readonly string profile;

    private string TypedField => FieldCombo.Text.Trim();

    // Filled on Save
    public JobApplication? Application { get; private set; }
    public string SourcePdf { get; private set; } = "";

    public NewApplicationWindow(ProfileStore store, string profile)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;
        FieldCombo.ItemsSource = store.ListFields(profile);
        FieldCombo.SelectedIndex = 0;
        ShowCvs();
        Loaded += (s, e) => CompanyBox.Focus();
    }

    private void FieldCombo_Changed(object sender, RoutedEventArgs e) => ShowCvs();

    // CVs made for that Field
    private void ShowCvs()
    {
        string field = FieldCombo.SelectedItem as string ?? TypedField;
        CvCombo.ItemsSource = field == "" ? new List<string>()
            : store.ListCvs(profile).Where(cv => !cv.Unreadable && string.Equals(cv.Field, NameCleaner.Clean(field), StringComparison.OrdinalIgnoreCase))
                .Select(cv => cv.Role).ToList();
        CvCombo.SelectedIndex = 0;
    }

    // Digits only, else null
    public static decimal? ParseAmount(string text)
    {
        string digits = new(text.Where(char.IsDigit).ToArray());
        return digits == "" ? null : decimal.Parse(digits);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string company = CompanyBox.Text.Trim();
        string field = NameCleaner.Clean(FieldCombo.SelectedItem as string ?? TypedField);
        if (company == "" || TypedField == "" || CvCombo.SelectedItem is not string role)
        {
            MessageBox.Show(this, Text.Get("Please enter the company, the Field and the CV."), "Fast Vitae");
            return;
        }

        var min = ParseAmount(MinBox.Text);
        var max = ParseAmount(MaxBox.Text);
        if (min != null && max != null && max < min)
            (min, max) = (max, min);

        string fullName = store.LoadProfile(profile).FullName;
        string? pdf = store.LatestPdf(profile, field, role, fullName);
        if (pdf == null)
        {
            MessageBox.Show(this, Text.Get("This CV has no PDF yet. Open it and click Create first."), "Fast Vitae");
            return;
        }

        SourcePdf = pdf;
        Application = new JobApplication
        {
            Field = field,
            Company = NameCleaner.Clean(company),
            Role = role,
            DateApplied = DateTime.Today,
            SalaryMin = min,
            SalaryMax = max,
            Currency = min == null && max == null ? "" : Search.GetValue(CurrencyCombo) is { Length: > 0 } code ? code : Model.Currency.Default.Code,
            Link = LinkBox.Text.Trim()
        };
        DialogResult = true;
    }
}
