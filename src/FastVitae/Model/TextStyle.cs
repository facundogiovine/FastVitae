namespace FastVitae.Model;

// Font, size and colour of one field
public class TextStyle
{
    public const string DefaultFont = "STIX Two Text";

    public static readonly string[] Fonts =
        { DefaultFont, "Garamond", "Georgia", "Cambria", "Times New Roman", "Calibri", "Arial" };

    public static readonly int[] Sizes = Enumerable.Range(8, 21).ToArray();

    // Black, grays, then sober colours
    public static readonly string[] Colors =
        { "#000000", "#404040", "#8C8C8C", "#1F2A44", "#1F4E79", "#1E6E6E", "#2E5E3A", "#7B1E2B", "#5C4033" };

    public string Font { get; set; } = DefaultFont;
    public int Size { get; set; } = 11;
    public string Color { get; set; } = "#000000";
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Strike { get; set; }

    // How each field prints untouched
    public static TextStyle DefaultFor(string key) => key switch
    {
        "Name" => new TextStyle { Size = 24, Bold = true },
        "Title" => new TextStyle { Size = 15 },
        "Summary" => new TextStyle { Italic = true },
        "SectionTitle" or "EntryName" or "RoleTitle" => new TextStyle { Bold = true },
        _ => new TextStyle()
    };

    public TextStyle Copy() => (TextStyle)MemberwiseClone();
}
