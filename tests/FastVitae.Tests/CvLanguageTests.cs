using System.Text.Json;
using FastVitae.Model;
using Xunit;

namespace FastVitae.Tests;

public class CvLanguageTests
{
    private static Role MayToPresent() => new() { StartMonth = "May", StartYear = "2020", EndYear = Role.Present };

    [Fact]
    public void DatesIn_Spanish_Present() =>
        Assert.Equal("Mayo 2020 - Actualidad", MayToPresent().DatesIn("es"));

    [Fact]
    public void DatesIn_English() =>
        Assert.Equal("May 2020 - Present", MayToPresent().DatesIn("en"));

    [Fact]
    public void NewFromScratch_Portuguese_Titles()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo(), "pt");
        Assert.Equal("pt", doc.Language);
        Assert.Equal(CvDocument.DefaultTitles.Select(t => Text.Get(t, "pt")), doc.Sections.Select(s => s.Title));
    }

    [Fact]
    public void SetLanguage_RenamesDefaultTitles()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo(), "en");
        doc.SetLanguage("es");
        Assert.Equal("es", doc.Language);
        Assert.Contains("EDUCACIÓN", doc.Sections.Select(s => s.Title));
    }

    [Fact]
    public void SetLanguage_KeepsCustomTitles()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo(), "en");
        doc.Sections.Add(new Section { Title = "PROJECTS" });
        doc.SetLanguage("es");
        Assert.Equal("PROJECTS", doc.Sections.Last().Title);
    }

    [Fact]
    public void SetLanguage_FromSpanishToPortuguese()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo(), "es");
        doc.SetLanguage("pt");
        Assert.Equal(CvDocument.DefaultTitles.Select(t => Text.Get(t, "pt")), doc.Sections.Select(s => s.Title));
    }

    [Fact]
    public void GuessLanguage_SpanishTitles() =>
        Assert.Equal("es", CvDocument.GuessLanguage(CvDocument.NewFromScratch(new BasicInfo(), "es"), "en"));

    [Fact]
    public void GuessLanguage_CustomOnly_ReturnsFallback()
    {
        var doc = new CvDocument();
        doc.Sections.Add(new Section { Title = "PROJECTS" });
        Assert.Equal("pt", CvDocument.GuessLanguage(doc, "pt"));
    }

    [Fact]
    public void Json_RoundTrip_KeepsLanguage()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo(), "pt");
        string json = JsonSerializer.Serialize(doc, CvDocument.JsonOptions);
        Assert.Equal("pt", JsonSerializer.Deserialize<CvDocument>(json, CvDocument.JsonOptions)!.Language);
    }
}

public class StartAsNewCvTests
{
    [Fact]
    public void StartAsNewCv_OldSource_GetsGrayDatesAndLanguage()
    {
        var doc = new CvDocument();
        doc.Sections.Add(new Section { Title = "EDUCATION" });
        doc.StartAsNewCv("es");
        Assert.Equal("EDUCACIÓN", doc.Sections[0].Title);
        Assert.Equal("#8C8C8C", doc.Styles["Dates"].Color);
    }

    [Fact]
    public void StartAsNewCv_KeepsSourceDateStyle()
    {
        var doc = new CvDocument();
        doc.Styles["Dates"] = new TextStyle { Color = "#1F4E79" };
        doc.StartAsNewCv("en");
        Assert.Equal("#1F4E79", doc.Styles["Dates"].Color);
    }
}
