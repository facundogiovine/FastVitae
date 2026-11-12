using System.IO;

namespace FastVitae.Storage;

public static class NameCleaner
{
    // Names Windows reserves
    private static readonly string[] Reserved =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    // Make a safe file name
    public static string Clean(string text)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var chars = (text ?? "").Select(c => invalid.Contains(c) ? '_' : c).ToArray();

        // Drop trailing dots and spaces
        string name = new string(chars).Trim().TrimEnd('.', ' ');

        if (name == "")
            return "Unnamed";
        if (Reserved.Contains(name.Split('.')[0].ToUpperInvariant()))
            return "_" + name;
        return name;
    }
}
