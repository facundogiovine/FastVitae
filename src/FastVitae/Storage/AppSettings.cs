using System.IO;
using System.Text.Json;

namespace FastVitae.Storage;

// Settings in AppData\Roaming\FastVitae
public class AppSettings
{
    // Full path, or empty
    public string DataFolder { get; set; } = "";
    public string LastProfile { get; set; } = "";

    // Empty: use data folder
    public string CvOutputFolder { get; set; } = "";

    // "es", "pt" or "en"
    public string Language { get; set; } = "";

    // Look online for new versions
    public bool CheckForUpdates { get; set; } = true;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FastVitae", "settings.json");

    // Where old versions kept it
    private static string OldFilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Fast Vitae");

    public void SetDataFolder(string fullPath) => DataFolder = Path.GetFullPath(fullPath);

    public static AppSettings Load() => Load(FilePath, OldFilePath);

    public static AppSettings Load(string file, string oldFile)
    {
        if (!File.Exists(file) && File.Exists(oldFile))
            MoveOldFile(file, oldFile);

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file)) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Broken settings rerun setup
            return new AppSettings();
        }
    }

    // Old relative paths become full
    private static void MoveOldFile(string file, string oldFile)
    {
        try
        {
            var old = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(oldFile)) ?? new AppSettings();
            if (old.DataFolder != "")
                old.DataFolder = Path.GetFullPath(old.DataFolder, Path.GetDirectoryName(oldFile)!);
            old.Save(file);
            File.Delete(oldFile);
        }
        catch (Exception)
        {
            // Leave it, setup runs
        }
    }

    public void Save() => Save(FilePath);

    public void Save(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
