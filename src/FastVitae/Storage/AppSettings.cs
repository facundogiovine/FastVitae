using System.IO;
using System.Text.Json;

namespace FastVitae.Storage;

// Settings next to the exe
public class AppSettings
{
    // Relative to the exe folder
    public string DataFolder { get; set; } = "";
    public string LastProfile { get; set; } = "";

    private static string AppFolder => AppContext.BaseDirectory;
    private static string FilePath => Path.Combine(AppFolder, "settings.json");

    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Fast Vitae");

    // Full path, or empty
    public string FullDataFolder => DataFolder == "" ? "" : Path.GetFullPath(DataFolder, AppFolder);

    // Keep inside paths relative
    public void SetDataFolder(string fullPath)
    {
        string relative = Path.GetRelativePath(AppFolder, fullPath);
        bool inside = !relative.StartsWith("..") && !Path.IsPathRooted(relative);
        DataFolder = inside ? relative : fullPath;
    }

    public static AppSettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Broken settings rerun setup
            return new AppSettings();
        }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
