using System.IO;
using System.Text.Json;
using FastVitae.Model;
using FastVitae.Storage;
using Xunit;

namespace FastVitae.Tests;

public class StorageTests : IDisposable
{
    private readonly string data = Directory.CreateTempSubdirectory("fv-store-").FullName;
    private readonly ProfileStore store;
    private const string Profile = "Ana Diaz";

    public StorageTests()
    {
        store = new ProfileStore(data);
        store.SaveProfile(new BasicInfo { FirstName = "Ana", LastName = "Diaz" });
    }

    public void Dispose() => Directory.Delete(data, recursive: true);

    private string ProfileFolder => store.ProfileFolder(Profile);

    private void WriteJson(string relative, CvDocument doc)
    {
        string path = Path.Combine(ProfileFolder, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(doc, CvDocument.JsonOptions));
    }

    private void WriteFile(string relative, string text)
    {
        string path = Path.Combine(ProfileFolder, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    // ---- CVs per Field ----

    [Fact]
    public void SaveCv_UnderFieldCvsFolder()
    {
        store.SaveCv(Profile, "Software Development", "C# Dev", new CvDocument());
        Assert.True(File.Exists(Path.Combine(ProfileFolder, "Fields", "Software Development", "CVs", "C# Dev.json")));
        Assert.True(store.CvExists(Profile, "Software Development", "C# Dev"));
    }

    [Fact]
    public void ListCvs_GivesFieldAndRole()
    {
        store.SaveCv(Profile, "Hospitality", "Waiter", new CvDocument());
        var item = Assert.Single(store.ListCvs(Profile));
        Assert.Equal("Hospitality", item.Field);
        Assert.Equal("Waiter", item.Role);
    }

    [Fact]
    public void Specialty_UnderSpecialtiesFolder()
    {
        store.SaveSpecialty(Profile, "IT", "C#", new CvDocument());
        Assert.True(File.Exists(Path.Combine(ProfileFolder, "Fields", "IT", "Specialties", "C#.json")));
        Assert.Equal(new[] { "C#" }, store.ListSpecialties(Profile, "IT"));
    }

    [Fact]
    public void OutputFolder_WithSaveFolder_ByField()
    {
        string root = Path.Combine(data, "Out");
        var withRoot = new ProfileStore(data) { CvOutputRoot = root };
        Assert.Equal(Path.Combine(root, Profile, "IT"), withRoot.OutputFolder(Profile, "IT"));
        Assert.Equal(Path.Combine(root, Profile, "Applications"), withRoot.ApplicationsFolder(Profile));
    }

    [Fact]
    public void LatestPdf_NewestOfThatRoleOnly()
    {
        string folder = store.OutputFolder(Profile, "IT");
        Directory.CreateDirectory(folder);
        void Pdf(string name, int minutesAgo)
        {
            string path = Path.Combine(folder, name + ".pdf");
            File.WriteAllText(path, name);
            File.SetLastWriteTime(path, DateTime.Now.AddMinutes(-minutesAgo));
        }
        Pdf("Ana Diaz - C# Dev", 30);
        Pdf("Ana Diaz - C# Dev_2", 10);
        Pdf("Ana Diaz - C# Developer", 1);
        Assert.EndsWith("Ana Diaz - C# Dev_2.pdf", store.LatestPdf(Profile, "IT", "C# Dev", "Ana Diaz"));
        Assert.Null(store.LatestPdf(Profile, "IT", "Waiter", "Ana Diaz"));
    }

    // ---- Moving old data ----

    [Fact]
    public void Migrate_MovesOldSpecialty()
    {
        WriteJson(Path.Combine("Fields", "IT", "C#.json"), new CvDocument());
        store.Migrate(Profile);
        Assert.Equal(new[] { "C#" }, store.ListSpecialties(Profile, "IT"));
        Assert.False(File.Exists(Path.Combine(ProfileFolder, "Fields", "IT", "C#.json")));
    }

    [Fact]
    public void Migrate_MovesOldCvToItsField()
    {
        WriteJson(Path.Combine("CVs", "Noir Company", "C# Dev.json"), new CvDocument { Field = "Software Development" });
        WriteFile(Path.Combine("CVs", "Noir Company", "Ana Diaz - C# Dev.pdf"), "pdf");
        store.Migrate(Profile);
        Assert.True(store.CvExists(Profile, "Software Development", "C# Dev"));
        Assert.True(File.Exists(Path.Combine(ProfileFolder, "Fields", "Software Development", "CVs", "Ana Diaz - C# Dev.pdf")));
        Assert.False(Directory.Exists(Path.Combine(ProfileFolder, "CVs")));
    }

    [Fact]
    public void Migrate_SameRoleTwice_GetsSuffix()
    {
        WriteJson(Path.Combine("CVs", "Acme", "Dev.json"), new CvDocument { Field = "IT" });
        WriteJson(Path.Combine("CVs", "Noir", "Dev.json"), new CvDocument { Field = "IT" });
        store.Migrate(Profile);
        Assert.Equal(new[] { "Dev", "Dev_2" }, store.ListCvs(Profile).Select(c => c.Role).OrderBy(r => r));
    }

    [Fact]
    public void Migrate_NoField_GoesToGeneral()
    {
        WriteJson(Path.Combine("CVs", "Acme", "Dev.json"), new CvDocument());
        store.Migrate(Profile);
        Assert.True(store.CvExists(Profile, "General", "Dev"));
    }

    [Fact]
    public void Migrate_FreezesOldApplicationPdf()
    {
        WriteJson(Path.Combine("CVs", "Noir Company", "C# Dev.json"), new CvDocument { Field = "IT" });
        WriteFile(Path.Combine("CVs", "Noir Company", "Ana Diaz - C# Dev.pdf"), "sent");
        var applications = new ApplicationStore(store);
        applications.Add(Profile, new JobApplication
        {
            Field = "IT", Company = "Noir Company", Role = "C# Dev", DateApplied = new DateTime(2026, 11, 13),
            CvPath = Path.Combine("CVs", "Noir Company", "Ana Diaz - C# Dev.pdf")
        }, sourcePdf: null);

        store.Migrate(Profile);

        var row = Assert.Single(applications.List(Profile));
        string frozen = Path.GetFullPath(row.CvPath, ProfileFolder);
        Assert.StartsWith(store.ApplicationsFolder(Profile), frozen);
        Assert.Equal("sent", File.ReadAllText(frozen));
    }

    [Fact]
    public void Migrate_TwiceChangesNothing()
    {
        WriteJson(Path.Combine("CVs", "Acme", "Dev.json"), new CvDocument { Field = "IT" });
        store.Migrate(Profile);
        store.Migrate(Profile);
        Assert.Single(store.ListCvs(Profile));
    }
}

public class ApplicationStoreTests : IDisposable
{
    private readonly string data = Directory.CreateTempSubdirectory("fv-apps-").FullName;
    private readonly ProfileStore store;
    private readonly ApplicationStore applications;
    private const string Profile = "Ana Diaz";

    public ApplicationStoreTests()
    {
        store = new ProfileStore(data);
        store.SaveProfile(new BasicInfo { FirstName = "Ana", LastName = "Diaz" });
        applications = new ApplicationStore(store);
    }

    public void Dispose() => Directory.Delete(data, recursive: true);

    private string SourcePdf(string text)
    {
        string path = Path.Combine(data, Guid.NewGuid() + ".pdf");
        File.WriteAllText(path, text);
        return path;
    }

    private static JobApplication Noir() => new()
    {
        Field = "IT", Company = "Noir Company", Role = "C# Dev", DateApplied = new DateTime(2026, 11, 16),
        SalaryMin = 2000, SalaryMax = 2500, Currency = "USD", Link = "https://www.linkedin.com/jobs/view/4491259278/"
    };

    [Fact]
    public void Add_CopiesPdfAndKeepsFields()
    {
        applications.Add(Profile, Noir(), SourcePdf("v1"));
        var row = Assert.Single(applications.List(Profile));
        Assert.Equal(2000, row.SalaryMin);
        Assert.Equal(2500, row.SalaryMax);
        Assert.Equal("USD", row.Currency);
        Assert.Equal("https://www.linkedin.com/jobs/view/4491259278/", row.Link);
        string frozen = Path.GetFullPath(row.CvPath, store.ProfileFolder(Profile));
        Assert.Equal(Path.Combine(store.ApplicationsFolder(Profile), "Noir Company - C# Dev - 2026-11-16.pdf"), frozen);
        Assert.Equal("v1", File.ReadAllText(frozen));
    }

    [Fact]
    public void Add_SameDayTwice_KeepsBothCopies()
    {
        applications.Add(Profile, Noir(), SourcePdf("v1"));
        applications.Add(Profile, Noir(), SourcePdf("v2"));
        var rows = applications.List(Profile);
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "v1", "v2" },
            rows.Select(r => File.ReadAllText(Path.GetFullPath(r.CvPath, store.ProfileFolder(Profile)))).OrderBy(t => t));
    }

    [Fact]
    public void SetStatus_ChangesOnlyThatRow()
    {
        applications.Add(Profile, Noir(), SourcePdf("v1"));
        applications.Add(Profile, Noir(), SourcePdf("v2"));
        var first = applications.List(Profile).First();
        applications.SetStatus(Profile, first, ApplicationStatus.Rejected);
        Assert.Single(applications.List(Profile), r => r.Status == ApplicationStatus.Rejected);
    }

    [Fact]
    public void SalaryText_RangeAndCurrency()
    {
        Assert.Equal("2,000 - 2,500 USD", Noir().SalaryTextIn(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("", new JobApplication().SalaryTextIn(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("3,000 USD", new JobApplication { SalaryMin = 3000, Currency = "USD" }.SalaryTextIn(System.Globalization.CultureInfo.InvariantCulture));
    }
}
