using FastVitae.Model;
using FastVitae.Rendering;
using Xunit;

namespace FastVitae.Tests;

public class ExampleTests
{
    [Fact]
    public void NewFromScratch_ExpertiseHasThreeEmptyBullets()
    {
        var expertise = CvDocument.NewFromScratch(new BasicInfo(), "en").Sections[0];
        var column = Assert.Single(expertise.Columns);
        Assert.Equal(3, column.Entries.Count);
        Assert.All(column.Entries, entry => Assert.Equal("", entry.Text));
    }

    [Fact]
    public void AddColumn_ComesWithOneBullet()
    {
        var section = new Section { Layout = SectionLayout.BulletColumns };
        var column = section.AddColumn();
        Assert.Single(column.Entries);
        Assert.Same(column, section.Columns.Last());
    }

    [Fact]
    public void AddColumn_StopsAtMax()
    {
        var section = new Section { Layout = SectionLayout.BulletColumns };
        for (int i = 0; i < Section.MaxColumns + 2; i++)
            section.AddColumn();
        Assert.Equal(Section.MaxColumns, section.Columns.Count);
    }

    [Fact]
    public void Preview_EmptySection_ShowsTitleAndExample()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo { FirstName = "Ana" }, "es");
        string html = HtmlRenderer.RenderBody(doc, examples: true);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("EDUCACIÓN"), html);
        Assert.Contains("Esto es un ejemplo", html);
    }

    [Fact]
    public void Print_EmptySection_Hidden()
    {
        var doc = CvDocument.NewFromScratch(new BasicInfo { FirstName = "Ana" }, "en");
        string html = HtmlRenderer.RenderBody(doc);
        Assert.DoesNotContain("EDUCATION", html);
        Assert.DoesNotContain("This is an example", html);
    }
}
