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
    public string Title { get; set; } = "";
    public string Dates { get; set; } = "";
    public List<string> Bullets { get; set; } = new();
}

// Company, school or project
public class Entry
{
    public string Name { get; set; } = "";
    public string Place { get; set; } = "";
    public ObservableCollection<Role> Roles { get; set; } = new();
}

// One block of the CV
public class Section
{
    public string Title { get; set; } = "";
    public SectionLayout Layout { get; set; }
    public List<string> Bullets { get; set; } = new();
    public ObservableCollection<Entry> Entries { get; set; } = new();
}

// A whole CV or Specialty
public class CvDocument
{
    public BasicInfo BasicInfo { get; set; } = new();

    // Show the profile picture
    public bool ShowPicture { get; set; }

    public string Summary { get; set; } = "";
    public ObservableCollection<Section> Sections { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    // Empty CV, standard sections
    public static CvDocument NewFromScratch(BasicInfo info)
    {
        var doc = new CvDocument { BasicInfo = info };
        doc.Sections.Add(new Section { Title = "AREAS OF EXPERTISE", Layout = SectionLayout.BulletColumns });
        doc.Sections.Add(new Section { Title = "PROFESSIONAL EXPERIENCE", Layout = SectionLayout.Entries });
        doc.Sections.Add(new Section { Title = "EDUCATION", Layout = SectionLayout.Entries });
        doc.Sections.Add(new Section { Title = "ADDITIONAL SKILLS", Layout = SectionLayout.Entries });
        return doc;
    }

    // Independent full copy
    public CvDocument Copy()
    {
        string json = JsonSerializer.Serialize(this, JsonOptions);
        return JsonSerializer.Deserialize<CvDocument>(json, JsonOptions)!;
    }
}
