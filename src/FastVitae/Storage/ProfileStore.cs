using System.IO;
using System.Text.Json;
using FastVitae.Model;

namespace FastVitae.Storage;

// One CV list row
public class CvListItem
{
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime LastEdit { get; set; }
    public string Path { get; set; } = "";
    // File could not be read
    public bool Unreadable { get; set; }
}

// Reads and writes profile data
public class ProfileStore
{
    private readonly string dataFolder;

    public ProfileStore(string dataFolder)
    {
        this.dataFolder = dataFolder;
    }

    private string ProfilesFolder => Path.Combine(dataFolder, "Profiles");
    public string ProfileFolder(string profile) => Path.Combine(ProfilesFolder, NameCleaner.Clean(profile));
    private string FieldsFolder(string profile) => Path.Combine(ProfileFolder(profile), "Fields");
    private string FieldFolder(string profile, string field) => Path.Combine(FieldsFolder(profile), NameCleaner.Clean(field));
    private string SpecialtyPath(string profile, string field, string specialty) =>
        Path.Combine(FieldFolder(profile, field), NameCleaner.Clean(specialty) + ".json");
    private string CvPath(string profile, string company, string role) =>
        Path.Combine(CvFolder(profile, company), NameCleaner.Clean(role) + ".json");

    // Set: PDF and Word go here
    public string CvOutputRoot { get; set; } = "";

    // Where PDF and Word go
    public string OutputFolder(string profile, string company) => CvOutputRoot == ""
        ? CvFolder(profile, company)
        : Path.Combine(CvOutputRoot, NameCleaner.Clean(profile), NameCleaner.Clean(company));

    // Output folder, else old place
    public string FolderToShow(string profile, string company) =>
        Directory.Exists(OutputFolder(profile, company)) ? OutputFolder(profile, company) : CvFolder(profile, company);

    // Top folder for the profile's CVs
    public string OutputRoot(string profile) => CvOutputRoot == ""
        ? Path.Combine(ProfileFolder(profile), "CVs")
        : Path.Combine(CvOutputRoot, NameCleaner.Clean(profile));

    public string CvFolder(string profile, string company) =>
        Path.Combine(ProfileFolder(profile), "CVs", NameCleaner.Clean(company));

    // ---- Profiles ----

    public List<string> ListProfiles()
    {
        if (!Directory.Exists(ProfilesFolder))
            return new List<string>();
        return Directory.GetDirectories(ProfilesFolder)
            .Where(folder => File.Exists(Path.Combine(folder, "profile.json")))
            .Select(folder => Path.GetFileName(folder))
            .OrderBy(name => name)
            .ToList();
    }

    public BasicInfo LoadProfile(string profile) =>
        Read<BasicInfo>(Path.Combine(ProfileFolder(profile), "profile.json"));

    // Returns the profile folder name
    public string SaveProfile(BasicInfo info)
    {
        string profile = NameCleaner.Clean(info.FullName);
        Write(Path.Combine(ProfileFolder(profile), "profile.json"), info);
        return profile;
    }

    // Profile picture
    public string PicturePath(string profile) => Path.Combine(ProfileFolder(profile), "picture.jpg");

    public bool HasPicture(string profile) => File.Exists(PicturePath(profile));

    // Old picture is kept aside
    public void SavePicture(string profile, string sourceFile)
    {
        // Resize first, so bad files keep the old
        string temp = PicturePath(profile) + ".new";
        PictureFile.SaveResized(sourceFile, temp);
        KeepOldPicture(profile);
        File.Move(temp, PicturePath(profile));
    }

    public void DeletePicture(string profile) => KeepOldPicture(profile);

    // Rename to First_Last_Picture_N
    private void KeepOldPicture(string profile)
    {
        if (!HasPicture(profile))
            return;
        string name;
        try
        {
            var info = LoadProfile(profile);
            name = $"{info.FirstName.Trim()}_{info.LastName.Trim()}";
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            name = profile;
        }
        name = NameCleaner.Clean(name.Replace(' ', '_'));

        int number = 1;
        while (File.Exists(Path.Combine(ProfileFolder(profile), $"{name}_Picture_{number}.jpg")))
            number++;
        File.Move(PicturePath(profile), Path.Combine(ProfileFolder(profile), $"{name}_Picture_{number}.jpg"));
    }

    public void RenameProfile(string profile, string newName) =>
        Directory.Move(ProfileFolder(profile), ProfileFolder(newName));

    public void DeleteProfile(string profile) => Directory.Delete(ProfileFolder(profile), recursive: true);

    // ---- Fields and Specialties ----

    public List<string> ListFields(string profile) => ListFolderNames(FieldsFolder(profile));

    public List<string> ListSpecialties(string profile, string field) => ListJsonNames(FieldFolder(profile, field));

    public void CreateField(string profile, string field) => Directory.CreateDirectory(FieldFolder(profile, field));

    public void RenameField(string profile, string field, string newName) =>
        Directory.Move(FieldFolder(profile, field), FieldFolder(profile, newName));

    public void DeleteField(string profile, string field) => Directory.Delete(FieldFolder(profile, field), recursive: true);

    public bool SpecialtyExists(string profile, string field, string specialty) =>
        File.Exists(SpecialtyPath(profile, field, specialty));

    public CvDocument LoadSpecialty(string profile, string field, string specialty) =>
        Read<CvDocument>(SpecialtyPath(profile, field, specialty));

    public void SaveSpecialty(string profile, string field, string specialty, CvDocument doc) =>
        Write(SpecialtyPath(profile, field, specialty), doc);

    public void RenameSpecialty(string profile, string field, string specialty, string newName) =>
        File.Move(SpecialtyPath(profile, field, specialty), SpecialtyPath(profile, field, newName));

    public void DeleteSpecialty(string profile, string field, string specialty) =>
        File.Delete(SpecialtyPath(profile, field, specialty));

    // ---- CVs ----

    public List<CvListItem> ListCvs(string profile)
    {
        var items = new List<CvListItem>();
        string cvsFolder = Path.Combine(ProfileFolder(profile), "CVs");
        foreach (string company in ListFolderNames(cvsFolder))
        {
            foreach (string file in Directory.GetFiles(Path.Combine(cvsFolder, company), "*.json"))
            {
                var item = new CvListItem
                {
                    Company = company,
                    Role = Path.GetFileNameWithoutExtension(file),
                    LastEdit = File.GetLastWriteTime(file),
                    Path = file
                };
                try
                {
                    Read<CvDocument>(file);
                }
                catch (Exception)
                {
                    item.Unreadable = true;
                }
                items.Add(item);
            }
        }
        return items;
    }

    public bool CvExists(string profile, string company, string role) => File.Exists(CvPath(profile, company, role));

    public CvDocument LoadCv(string profile, string company, string role) => Read<CvDocument>(CvPath(profile, company, role));

    public void SaveCv(string profile, string company, string role, CvDocument doc) => Write(CvPath(profile, company, role), doc);

    // ---- Helpers ----

    private static List<string> ListFolderNames(string folder)
    {
        if (!Directory.Exists(folder))
            return new List<string>();
        return Directory.GetDirectories(folder).Select(f => Path.GetFileName(f)).OrderBy(n => n).ToList();
    }

    private static List<string> ListJsonNames(string folder)
    {
        if (!Directory.Exists(folder))
            return new List<string>();
        return Directory.GetFiles(folder, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(n => n).ToList();
    }

    private static T Read<T>(string path)
    {
        var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), CvDocument.JsonOptions);
        return value ?? throw new InvalidDataException($"{path} is empty.");
    }

    private static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, CvDocument.JsonOptions));
    }
}
