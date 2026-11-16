using FastVitae.Model;
using FastVitae.Rendering;
using Xunit;

namespace FastVitae.Tests;

public class DateStyleTests
{
    private static CvDocument ScratchWithRole(out Role role)
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo { FirstName = "Ana" }, "en");
        role = new Role { Title = "Dev", StartMonth = "May", StartYear = "2020", EndYear = Role.Present };
        var entry = new Entry { Name = "Acme" };
        entry.Roles.Add(role);
        doc.Sections.First(s => s.Layout == SectionLayout.Entries).Entries.Add(entry);
        return doc;
    }

    [Fact]
    public void NewFromScratch_SetsGrayItalicDates()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo(), "en");
        Assert.True(doc.Styles["Dates"].Italic);
        Assert.Equal("#8C8C8C", doc.Styles["Dates"].Color);
    }

    [Fact]
    public void DateStyle_RoleOverridesDocument()
    {
        var doc = ScratchWithRole(out var role);
        role.Styles["Dates"] = new TextStyle { Color = "#1F4E79" };
        Assert.Equal("#1F4E79", doc.DateStyle(role)!.Color);
    }

    [Fact]
    public void DateStyle_OldDocument_IsNull()
    {
        var doc = new CvDocument();
        Assert.Null(doc.DateStyle(new Role()));
    }

    [Fact]
    public void Html_NewCv_DatesGrayItalic()
    {
        var doc = ScratchWithRole(out _);
        string html = HtmlRenderer.RenderBody(doc);
        int dates = html.IndexOf("May 2020");
        string before = html[..dates];
        string tag = before[before.LastIndexOf('<')..];
        Assert.Contains("color: #8C8C8C", tag);
        Assert.Contains("font-style: italic", tag);
    }
}
