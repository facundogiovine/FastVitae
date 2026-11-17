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
    public static string RenderBody(CvDocument doc, string? picturePath = null, bool examples = false)
    {
        var html = new StringBuilder();
        var info = doc.BasicInfo;

        string nameAndTitle = $"<h1{Css(doc.Styles, "Name")}>{Text(info.FullName)}</h1>";
        if (HasText(info.Title))
            nameAndTitle += $"<p class=\"title\"{Css(doc.Styles, "Title")}>{Text(info.Title)}</p>";

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
            html.Append($"<p class=\"contact\"{Css(doc.Styles, "Contact")}>{contact}</p>");

        if (HasText(doc.Summary))
            html.Append($"<p class=\"summary\"{Css(doc.Styles, "Summary")}>{Text(doc.Summary)}</p>");

        foreach (var section in doc.Sections)
        {
            string content = section.Layout == SectionLayout.BulletColumns
                ? ColumnsHtml(section)
                : string.Concat(section.Entries.Select(entry => EntryHtml(doc, entry)));

            // Empty: example, or skip
            if (content == "" && examples)
                content = $"<p class=\"example\">{Text(FastVitae.Text.Get("This is an example", doc.Language))}</p>";
            if (content == "")
                continue;
            html.Append($"<h2{Css(section.Styles, "SectionTitle")}>{Text(section.Title)}</h2>");
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

    // Side by side lists
    private static string ColumnsHtml(Section section)
    {
        var lists = section.Columns
            .Select(column => ColumnList(column))
            .Where(list => list != "")
            .ToList();
        return lists.Count == 0 ? "" : $"<div class=\"columns\">{string.Concat(lists.Select(list => $"<div>{list}</div>"))}</div>";
    }

    private static string EntryHtml(CvDocument doc, Entry entry)
    {
        var html = new StringBuilder();
        if (HasText(entry.Name) || HasText(entry.Place))
            html.Append(Line("entry-name", entry.Name, entry.Place, Css(entry.Styles, "EntryName"), Css(entry.Styles, "Place")));

        foreach (var role in entry.Roles)
        {
            if (HasText(role.Title) || HasText(role.DatesIn(doc.Language)))
                html.Append(Line("role-title", role.Title, role.DatesIn(doc.Language), Css(role.Styles, "RoleTitle"), Css(doc.DateStyle(role))));
            html.Append(BulletList(role.Bullets, Css(role.Styles, "Bullets")));
        }
        return html.Length == 0 ? "" : $"<div class=\"entry\">{html}</div>";
    }

    private static string Line(string leftClass, string left, string right, string leftCss, string rightCss) =>
        $"<div class=\"line\"><span class=\"{leftClass}\"{leftCss}>{Text(left)}</span><span class=\"right\"{rightCss}>{Text(right)}</span></div>";

    private static string BulletList(List<string> bullets, string css)
    {
        var items = bullets.Where(HasText).Select(b => $"<li>{Text(b)}</li>").ToList();
        return items.Count == 0 ? "" : $"<ul{css}>{string.Concat(items)}</ul>";
    }

    // Each bullet its own style
    private static string ColumnList(BulletColumn column)
    {
        var items = column.Entries.Where(entry => HasText(entry.Text))
            .Select(entry => $"<li{Css(entry.Styles, "Bullet")}>{Text(entry.Text)}</li>").ToList();
        return items.Count == 0 ? "" : $"<ul>{string.Concat(items)}</ul>";
    }

    // Inline CSS for style
    private static string Css(Dictionary<string, TextStyle> styles, string key) =>
        Css(styles.TryGetValue(key, out var style) ? style : null);

    private static string Css(TextStyle? style)
    {
        if (style == null)
            return "";
        return $" style=\"font-family: '{style.Font}', 'STIX Two Text', serif; font-size: {style.Size}pt; color: {style.Color}; " +
            $"font-weight: {(style.Bold ? "bold" : "normal")}; font-style: {(style.Italic ? "italic" : "normal")}; " +
            $"text-decoration: {(style.Strike ? "line-through" : "none")};\"";
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
