using System.IO;
using System.Net;
using System.Text;
using FastVitae.Model;

namespace FastVitae.Rendering;

// CV to HTML
public static class HtmlRenderer
{
    // Full page with styles
    public static string RenderPage(CvDocument doc, string? picturePath = null)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>");
        html.Append(FontFace("regular", "normal", "normal"));
        html.Append(FontFace("bold", "bold", "normal"));
        html.Append(FontFace("italic", "normal", "italic"));
        html.Append(FontFace("boldItalic", "bold", "italic"));
        html.Append(ReadCss());
        html.Append("</style></head><body><div id=\"page\">");
        html.Append(RenderBody(doc, picturePath));
        html.Append("</div></body></html>");
        return html.ToString();
    }

    // Only the CV content
    public static string RenderBody(CvDocument doc, string? picturePath = null)
    {
        var html = new StringBuilder();
        var info = doc.BasicInfo;

        string nameAndTitle = $"<h1>{Text(info.FullName)}</h1>";
        if (HasText(info.Title))
            nameAndTitle += $"<p class=\"title\">{Text(info.Title)}</p>";

        if (UsesPicture(doc, picturePath))
        {
            // Name left, picture right
            string base64 = Convert.ToBase64String(File.ReadAllBytes(picturePath!));
            html.Append($"<div class=\"pictured\"><div>{nameAndTitle}</div>");
            html.Append($"<img class=\"picture\" src=\"data:image/jpeg;base64,{base64}\"></div>");
        }
        else
        {
            html.Append(nameAndTitle);
        }

        string contact = ContactLine(info);
        if (contact != "")
            html.Append($"<p class=\"contact\">{contact}</p>");

        if (HasText(doc.Summary))
            html.Append($"<p class=\"summary\">{Text(doc.Summary)}</p>");

        foreach (var section in doc.Sections)
        {
            string content = section.Layout == SectionLayout.BulletColumns
                ? BulletList(section.Bullets, "columns")
                : string.Concat(section.Entries.Select(EntryHtml));

            // Skip empty sections
            if (content == "")
                continue;
            html.Append($"<h2>{Text(section.Title)}</h2>");
            html.Append(content);
        }
        return html.ToString();
    }

    // Picture wanted and present
    public static bool UsesPicture(CvDocument doc, string? picturePath) =>
        doc.ShowPicture && picturePath != null && File.Exists(picturePath);

    // Country code plus number
    public static string PhoneText(BasicInfo info) => $"{info.CountryCode} {info.Phone}".Trim();

    // Make the link clickable
    public static string LinkUrl(string link)
    {
        link = link.Trim();
        return link.StartsWith("http://") || link.StartsWith("https://") ? link : "https://" + link;
    }

    private static string ContactLine(BasicInfo info)
    {
        var parts = new List<string>();
        if (HasText(info.Location)) parts.Add(Text(info.Location));
        if (PhoneText(info) != "") parts.Add(Text(PhoneText(info)));
        if (HasText(info.Email)) parts.Add(Text(info.Email));
        if (HasText(info.LinkedIn))
            parts.Add($"<a href=\"{Text(LinkUrl(info.LinkedIn))}\">{Text(info.LinkedIn)}</a>");
        return string.Join(" | ", parts);
    }

    private static string EntryHtml(Entry entry)
    {
        var html = new StringBuilder();
        if (HasText(entry.Name) || HasText(entry.Place))
            html.Append(Line("entry-name", entry.Name, entry.Place));

        foreach (var role in entry.Roles)
        {
            if (HasText(role.Title) || HasText(role.Dates))
                html.Append(Line("role-title", role.Title, role.Dates));
            html.Append(BulletList(role.Bullets, ""));
        }
        return html.Length == 0 ? "" : $"<div class=\"entry\">{html}</div>";
    }

    private static string Line(string leftClass, string left, string right) =>
        $"<div class=\"line\"><span class=\"{leftClass}\">{Text(left)}</span><span class=\"right\">{Text(right)}</span></div>";

    private static string BulletList(List<string> bullets, string cssClass)
    {
        var items = bullets.Where(HasText).Select(b => $"<li>{Text(b)}</li>").ToList();
        if (items.Count == 0)
            return "";
        string classPart = cssClass == "" ? "" : $" class=\"{cssClass}\"";
        return $"<ul{classPart}>{string.Concat(items)}</ul>";
    }

    private static bool HasText(string? text) => !string.IsNullOrWhiteSpace(text);

    // Escape user text
    private static string Text(string? text) => WebUtility.HtmlEncode((text ?? "").Trim());

    // Font embedded in the page
    private static string FontFace(string file, string weight, string style)
    {
        string base64 = Convert.ToBase64String(ReadResource($"FastVitae.Fonts.STIXTwoText-{file}.ttf"));
        return $"@font-face {{ font-family: \"STIX Two Text\"; font-weight: {weight}; font-style: {style}; " +
               $"src: url(data:font/ttf;base64,{base64}); }}\n";
    }

    private static string ReadCss() => Encoding.UTF8.GetString(ReadResource("FastVitae.Rendering.cv.css"));

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(name)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
