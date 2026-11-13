using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FastVitae.Model;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace FastVitae.Rendering;

// CV to Word file
public class DocxWriter
{
    private const string Font = "STIX Two Text";
    private const string Gray = "A0A0A0";
    private const string LinkBlue = "1155CC";

    private const int PageWidth = 12240;    // 8.5in
    private const int PageHeight = 15840;   // 11in
    private const int MarginTopBottom = 720;    // 0.5in
    private const int MarginLeftRight = 576;    // 0.4in
    private const int TextWidth = PageWidth - 2 * MarginLeftRight;

    private const int BulletListId = 1;

    private readonly MainDocumentPart mainPart;
    private readonly Body body = new();

    private const int PictureColumnWidth = 2400;
    private const long PictureHeightEmu = 1143000;  // 1.25in in EMU

    public static void Write(CvDocument doc, string path, string? picturePath = null)
    {
        using var file = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var writer = new DocxWriter(file.AddMainDocumentPart());
        writer.Build(doc, picturePath);
    }

    private DocxWriter(MainDocumentPart mainPart)
    {
        this.mainPart = mainPart;
    }

    private void Build(CvDocument doc, string? picturePath)
    {
        AddBulletDefinition();
        AddHeader(doc, picturePath);

        foreach (var section in doc.Sections)
        {
            if (section.Layout == SectionLayout.BulletColumns)
                AddBulletColumns(section);
            else
                AddEntries(section);
        }

        body.Append(new SectionProperties(
            new PageSize { Width = PageWidth, Height = PageHeight },
            new PageMargin
            {
                Top = MarginTopBottom, Bottom = MarginTopBottom,
                Left = MarginLeftRight, Right = MarginLeftRight,
                Header = 144, Footer = 288, Gutter = 0
            }));
        mainPart.Document = new Document(body);
    }

    // ---- Parts of the CV ----

    private void AddHeader(CvDocument doc, string? picturePath)
    {
        var info = doc.BasicInfo;
        bool pictured = HtmlRenderer.UsesPicture(doc, picturePath);

        var nameAndTitle = new List<Paragraph>
        {
            Paragraph(center: !pictured, spaceAfter: 0, Run(info.FullName, bold: true, size: 48, style: StyleOf(doc.Styles, "Name")))
        };
        if (HasText(info.Title))
            nameAndTitle.Add(Paragraph(center: !pictured, spaceAfter: pictured ? 0 : 80, Run(info.Title, size: 30, style: StyleOf(doc.Styles, "Title"))));

        if (pictured)
            body.Append(PicturedHeader(nameAndTitle, picturePath!));
        else
            body.Append(nameAndTitle);

        var contact = ContactRuns(info, StyleOf(doc.Styles, "Contact"));
        if (contact.Count > 0)
        {
            var paragraph = Paragraph(center: true, spaceAfter: 160, contact.ToArray());
            paragraph.ParagraphProperties!.ParagraphBorders = new ParagraphBorders(
                new TopBorder { Val = BorderValues.Single, Size = 12, Space = 4, Color = Gray },
                new BottomBorder { Val = BorderValues.Single, Size = 12, Space = 4, Color = Gray });
            body.Append(paragraph);
        }

        if (HasText(doc.Summary))
            body.Append(Paragraph(center: false, spaceAfter: 120, Run(doc.Summary, italic: true, style: StyleOf(doc.Styles, "Summary"))));
    }

    // Borderless two-cell header table
    private Table PicturedHeader(List<Paragraph> nameAndTitle, string picturePath)
    {
        int leftWidth = TextWidth - PictureColumnWidth;

        var leftCell = new TableCell(new TableCellProperties(
            new TableCellWidth { Width = leftWidth.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
        leftCell.Append(nameAndTitle);

        var picture = Paragraph(center: false, spaceAfter: 0, new Run(Picture(picturePath)));
        picture.ParagraphProperties!.Justification = new Justification { Val = JustificationValues.Right };
        var rightCell = new TableCell(
            new TableCellProperties(new TableCellWidth { Width = PictureColumnWidth.ToString(), Type = TableWidthUnitValues.Dxa }),
            picture);

        return new Table(
            new TableProperties(
                new TableWidth { Width = TextWidth.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableLayout { Type = TableLayoutValues.Fixed },
                // No cell padding
                new TableCellMarginDefault(
                    new TableCellLeftMargin { Width = 0, Type = TableWidthValues.Dxa },
                    new TableCellRightMargin { Width = 0, Type = TableWidthValues.Dxa })),
            new TableGrid(
                new GridColumn { Width = leftWidth.ToString() },
                new GridColumn { Width = PictureColumnWidth.ToString() }),
            new TableRow(leftCell, rightCell));
    }

    // Embed and show the picture
    private Drawing Picture(string picturePath)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
        using (var stream = File.OpenRead(picturePath))
            imagePart.FeedData(stream);

        // Fixed height, proportional width
        var size = FastVitae.Storage.PictureFile.Load(picturePath);
        long height = PictureHeightEmu;
        long width = height * size.PixelWidth / size.PixelHeight;

        var graphic = new A.Graphic(new A.GraphicData(
            new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = "picture.jpg" },
                    new PIC.NonVisualPictureDrawingProperties()),
                new PIC.BlipFill(
                    new A.Blip { Embed = mainPart.GetIdOfPart(imagePart) },
                    new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                    new A.Transform2D(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = width, Cy = height }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" });

        return new Drawing(new DW.Inline(
            new DW.Extent { Cx = width, Cy = height },
            new DW.DocProperties { Id = 1U, Name = "Picture" },
            graphic)
        { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });
    }

    private List<OpenXmlElement> ContactRuns(BasicInfo info, TextStyle? style)
    {
        var parts = new List<OpenXmlElement>();
        if (HasText(info.Location)) parts.Add(Run(info.Location, style: style));
        if (HtmlRenderer.PhoneText(info) != "") parts.Add(Run(HtmlRenderer.PhoneText(info), style: style));
        if (HasText(info.Email)) parts.Add(Run(info.Email, style: style));
        if (HasText(info.LinkedIn)) parts.Add(Link(info.LinkedIn));

        // Separate parts with a bar
        var runs = new List<OpenXmlElement>();
        foreach (var part in parts)
        {
            if (runs.Count > 0)
                runs.Add(Run(" | ", style: style));
            runs.Add(part);
        }
        return runs;
    }

    private void AddBulletColumns(Section section)
    {
        var columns = section.Columns
            .Select(column => column.Entries.Where(entry => HasText(entry.Text)).ToList())
            .Where(bullets => bullets.Count > 0)
            .ToList();
        if (columns.Count == 0)
            return;
        AddSectionTitle(section.Title, StyleOf(section.Styles, "SectionTitle"));

        // Borderless table, one row
        string cellWidth = (TextWidth / columns.Count).ToString();
        var table = new Table(new TableProperties(
            new TableWidth { Width = TextWidth.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed }));
        table.Append(new TableGrid(columns.Select(_ => new GridColumn { Width = cellWidth })));

        var row = new TableRow();
        foreach (var bullets in columns)
        {
            var cell = new TableCell(new TableCellProperties(
                new TableCellWidth { Width = cellWidth, Type = TableWidthUnitValues.Dxa }));
            cell.Append(bullets.Select(entry => Bullet(entry.Text, StyleOf(entry.Styles, "Bullet"))));
            row.Append(cell);
        }
        table.Append(row);
        body.Append(table);
    }

    private void AddEntries(Section section)
    {
        var paragraphs = new List<Paragraph>();
        foreach (var entry in section.Entries)
        {
            var entryParagraphs = new List<Paragraph>();
            if (HasText(entry.Name) || HasText(entry.Place))
                entryParagraphs.Add(LeftRightLine(entry.Name, entry.Place, StyleOf(entry.Styles, "EntryName"), StyleOf(entry.Styles, "Place")));

            foreach (var role in entry.Roles)
            {
                if (HasText(role.Title) || HasText(role.Dates))
                    entryParagraphs.Add(LeftRightLine(role.Title, role.Dates, StyleOf(role.Styles, "RoleTitle"), StyleOf(role.Styles, "Dates")));
                entryParagraphs.AddRange(role.Bullets.Where(HasText).Select(text => Bullet(text, StyleOf(role.Styles, "Bullets"))));
            }

            // Gap after each entry
            if (entryParagraphs.Count > 0)
                entryParagraphs[^1].ParagraphProperties!.SpacingBetweenLines!.After = "200";
            paragraphs.AddRange(entryParagraphs);
        }

        if (paragraphs.Count == 0)
            return;
        AddSectionTitle(section.Title, StyleOf(section.Styles, "SectionTitle"));
        body.Append(paragraphs);
    }

    // Bold title with underline
    private void AddSectionTitle(string title, TextStyle? style)
    {
        var paragraph = Paragraph(center: false, spaceAfter: 160, Run(title, bold: true, style: style));
        paragraph.ParagraphProperties!.SpacingBetweenLines!.Before = "240";
        paragraph.ParagraphProperties.KeepNext = new KeepNext();
        paragraph.ParagraphProperties.ParagraphBorders = new ParagraphBorders(
            new BottomBorder { Val = BorderValues.Single, Size = 8, Space = 1, Color = "000000" });
        body.Append(paragraph);
    }

    // ---- Small building blocks ----

    // Bold left, plain right
    private static Paragraph LeftRightLine(string left, string right, TextStyle? leftStyle, TextStyle? rightStyle)
    {
        var paragraph = Paragraph(center: false, spaceAfter: 0,
            Run(left, bold: true, style: leftStyle), new Run(new TabChar()), Run(right, style: rightStyle));
        paragraph.ParagraphProperties!.Tabs = new Tabs(
            new TabStop { Val = TabStopValues.Right, Position = TextWidth });
        paragraph.ParagraphProperties.KeepNext = new KeepNext();
        return paragraph;
    }

    private static Paragraph Bullet(string text, TextStyle? style)
    {
        var paragraph = Paragraph(center: false, spaceAfter: 0, Run(text, style: style));
        paragraph.ParagraphProperties!.NumberingProperties = new NumberingProperties(
            new NumberingLevelReference { Val = 0 },
            new NumberingId { Val = BulletListId });
        return paragraph;
    }

    private static Paragraph Paragraph(bool center, int spaceAfter, params OpenXmlElement[] runs)
    {
        var properties = new ParagraphProperties
        {
            SpacingBetweenLines = new SpacingBetweenLines { Before = "0", After = spaceAfter.ToString() }
        };
        if (center)
            properties.Justification = new Justification { Val = JustificationValues.Center };

        var paragraph = new Paragraph(properties);
        paragraph.Append(runs);
        return paragraph;
    }

    private static Run Run(string text, bool bold = false, bool italic = false, int size = 22, TextStyle? style = null)
    {
        return new Run(
            style == null ? RunStyle(bold, italic, size) : RunStyle(style),
            new WordText(text.Trim() == "|" ? text : text.Trim()) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static RunProperties RunStyle(bool bold, bool italic, int size)
    {
        var properties = new RunProperties(
            new RunFonts { Ascii = Font, HighAnsi = Font, EastAsia = Font, ComplexScript = Font });
        if (bold) properties.Append(new Bold());
        if (italic) properties.Append(new Italic());
        properties.Append(new FontSize { Val = size.ToString() });
        properties.Append(new FontSizeComplexScript { Val = size.ToString() });
        return properties;
    }

    // User-set field style
    private static TextStyle? StyleOf(Dictionary<string, TextStyle> styles, string key) =>
        styles.TryGetValue(key, out var style) ? style : null;

    private static RunProperties RunStyle(TextStyle style)
    {
        var properties = new RunProperties(
            new RunFonts { Ascii = style.Font, HighAnsi = style.Font, EastAsia = style.Font, ComplexScript = style.Font });
        if (style.Bold) properties.Append(new Bold());
        if (style.Italic) properties.Append(new Italic());
        if (style.Strike) properties.Append(new Strike());
        properties.Append(new Color { Val = style.Color.TrimStart('#') });
        properties.Append(new FontSize { Val = (style.Size * 2).ToString() });
        properties.Append(new FontSizeComplexScript { Val = (style.Size * 2).ToString() });
        return properties;
    }

    // Clickable blue link
    private Hyperlink Link(string link)
    {
        var relationship = mainPart.AddHyperlinkRelationship(new Uri(HtmlRenderer.LinkUrl(link)), isExternal: true);

        var style = RunStyle(bold: false, italic: false, size: 22);
        style.InsertAfter(new Color { Val = LinkBlue }, style.GetFirstChild<RunFonts>());
        style.Append(new Underline { Val = UnderlineValues.Single });

        return new Hyperlink(new Run(style, new WordText(link.Trim()))) { Id = relationship.Id };
    }

    // Define the bullet style
    private void AddBulletDefinition()
    {
        var level = new Level(
            new StartNumberingValue { Val = 1 },
            new NumberingFormat { Val = NumberFormatValues.Bullet },
            new LevelText { Val = "•" },
            new LevelJustification { Val = LevelJustificationValues.Left },
            new PreviousParagraphProperties(new Indentation { Left = "720", Hanging = "360" }))
        { LevelIndex = 0 };

        var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
        numberingPart.Numbering = new Numbering(
            new AbstractNum(new MultiLevelType { Val = MultiLevelValues.HybridMultilevel }, level) { AbstractNumberId = 0 },
            new NumberingInstance(new AbstractNumId { Val = 0 }) { NumberID = BulletListId });
    }

    private static bool HasText(string? text) => !string.IsNullOrWhiteSpace(text);
}
