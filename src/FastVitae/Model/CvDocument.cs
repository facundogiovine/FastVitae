using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastVitae.Model;

public enum SectionLayout
{
    // Bullets in three columns
    BulletColumns,
    // Named entries with roles
    Entries
}

// Contact details
public class BasicInfo
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";

    // Picking a country sets code
    private string country = Model.Country.DefaultIso;
    public string Country
    {
        get => country;
        set
        {
            country = value;
            var picked = Model.Country.Find(value);
            if (picked != null)
                CountryCode = picked.DialCode;
        }
    }

    public string CountryCode { get; set; } = Model.Country.Find(Model.Country.DefaultIso)!.DialCode;
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string LinkedIn { get; set; } = "";

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}

// Title, dates and bullets
public class Role
{
    public const string Present = "Present";

    // Dropdown choices, blank first
    public static readonly string[] Months =
        { "", "January", "February", "March", "April", "May", "June",
          "July", "August", "September", "October", "November", "December" };
    public static readonly string[] Years =
        new[] { "" }.Concat(Enumerable.Range(DateTime.Now.Year - 60, 67).Reverse().Select(y => y.ToString())).ToArray();
    public static readonly string[] EndYears = new[] { "", Present }.Concat(Years.Skip(1)).ToArray();

    public string Title { get; set; } = "";
    public string StartMonth { get; set; } = "";
    public string StartYear { get; set; } = "";
    public string EndMonth { get; set; } = "";
    public string EndYear { get; set; } = "";
    public List<string> Bullets { get; set; } = new();

    // RoleTitle, Dates, Bullets
    public Dictionary<string, TextStyle> Styles { get; set; } = new();

    // Dates as printed
    public string DatesIn(string language)
    {
        string start = $"{Text.Get(StartMonth, language)} {StartYear}".Trim();
        string end = EndYear == Present ? Text.Get(Present, language) : $"{Text.Get(EndMonth, language)} {EndYear}".Trim();
        return start != "" && end != "" ? $"{start} - {end}" : start + end;
    }

    // Old files: free text dates
    [JsonPropertyName("dates")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OldDates
    {
        get => null;
        set
        {
            var parts = (value ?? "").Split('-', '–');
            (StartMonth, StartYear) = ParseDate(parts[0]);
            if (parts.Length > 1)
                (EndMonth, EndYear) = ParseDate(parts[1]);
        }
    }

    // "May 2020" to month, year
    private static (string month, string year) ParseDate(string text)
    {
        text = text.Trim();
        if (text.Equals(Present, StringComparison.OrdinalIgnoreCase))
            return ("", Present);
        string month = Months.FirstOrDefault(m => m != "" && text.StartsWith(m[..3], StringComparison.OrdinalIgnoreCase)) ?? "";
        string year = text.Split(' ').FirstOrDefault(word => word.Length == 4 && word.All(char.IsDigit)) ?? "";
        return (month, year);
    }
}

// Company, school or project
public class Entry
{
    public string Name { get; set; } = "";
    public string Place { get; set; } = "";
    public ObservableCollection<Role> Roles { get; set; } = new();

    // EntryName, Place
    public Dictionary<string, TextStyle> Styles { get; set; } = new();
}

// One bullet in a column
public class ColumnEntry
{
    public string Text { get; set; } = "";

    // Bullet
    public Dictionary<string, TextStyle> Styles { get; set; } = new();
}

// One column of bullets
public class BulletColumn
{
    public ObservableCollection<ColumnEntry> Entries { get; set; } = new();
}

// One block of the CV
public class Section
{
    public const int MaxColumns = 4;

    public string Title { get; set; } = "";
    public SectionLayout Layout { get; set; }
    public ObservableCollection<BulletColumn> Columns { get; set; } = new();
    public ObservableCollection<Entry> Entries { get; set; } = new();

    // SectionTitle
    public Dictionary<string, TextStyle> Styles { get; set; } = new();

    // Old files: split into three
    [JsonPropertyName("bullets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? OldBullets
    {
        get => null;
        set
        {
            if (value == null || value.Count == 0 || Columns.Count > 0)
                return;
            for (int i = 0; i < 3; i++)
                Columns.Add(new BulletColumn());
            for (int i = 0; i < value.Count; i++)
                Columns[i % 3].Entries.Add(new ColumnEntry { Text = value[i] });
        }
    }
}

// A whole CV or Specialty
public class CvDocument
{
    public BasicInfo BasicInfo { get; set; } = new();

    // Field this CV applies under
    public string Field { get; set; } = "";

    // Show the profile picture
    public bool ShowPicture { get; set; }

    // "es", "pt" or "en"
    public string Language { get; set; } = "";

    public string Summary { get; set; } = "";

    // Name, Title, Contact, Summary
    public Dictionary<string, TextStyle> Styles { get; set; } = new();
    public ObservableCollection<Section> Sections { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    // Standard section titles, English
    public static readonly string[] DefaultTitles =
        { "AREAS OF EXPERTISE", "PROFESSIONAL EXPERIENCE", "EDUCATION", "ADDITIONAL SKILLS" };

    // Empty CV, standard sections
    public static CvDocument NewFromScratch(BasicInfo info, string language)
    {
        var doc = new CvDocument { BasicInfo = info, Language = language };
        doc.Styles["Dates"] = TextStyle.NewCvDates;
        var expertise = new Section { Title = Text.Get(DefaultTitles[0], language), Layout = SectionLayout.BulletColumns };
        expertise.Columns.Add(new BulletColumn());
        doc.Sections.Add(expertise);
        foreach (string title in DefaultTitles.Skip(1))
            doc.Sections.Add(new Section { Title = Text.Get(title, language), Layout = SectionLayout.Entries });
        return doc;
    }

    // Switch language, rename default titles
    public void SetLanguage(string code)
    {
        Language = code;
        foreach (var section in Sections)
        {
            string? english = DefaultTitles.FirstOrDefault(title =>
                Text.Languages.Any(l => Text.Get(title, l.Code) == section.Title));
            if (english != null)
                section.Title = Text.Get(english, code);
        }
    }

    // Language most titles are in
    public static string GuessLanguage(CvDocument doc, string fallback)
    {
        var counts = Text.Languages
            .Select(l => (l.Code, Count: doc.Sections.Count(s => DefaultTitles.Any(t => Text.Get(t, l.Code) == s.Title))))
            .OrderByDescending(c => c.Count)
            .ToList();
        bool clearWinner = counts[0].Count > 0 && counts[0].Count > counts[1].Count;
        return clearWinner ? counts[0].Code : fallback;
    }

    // Role style, else CV default
    public TextStyle? DateStyle(Role role) =>
        role.Styles.TryGetValue("Dates", out var own) ? own
        : Styles.TryGetValue("Dates", out var shared) ? shared
        : null;

    // Independent full copy
    public CvDocument Copy()
    {
        string json = JsonSerializer.Serialize(this, JsonOptions);
        return JsonSerializer.Deserialize<CvDocument>(json, JsonOptions)!;
    }
}
