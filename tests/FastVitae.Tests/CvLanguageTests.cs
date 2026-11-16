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
