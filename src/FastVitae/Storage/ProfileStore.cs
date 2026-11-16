using System.IO;
using System.Text.Json;
using FastVitae.Model;

namespace FastVitae.Storage;

// One CV list row
public class CvListItem
{
    public string Field { get; set; } = "";
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
    private string SpecialtiesFolder(string profile, string field) => Path.Combine(FieldFolder(profile, field), "Specialties");
    private string SpecialtyPath(string profile, string field, string specialty) =>
        Path.Combine(SpecialtiesFolder(profile, field), NameCleaner.Clean(specialty) + ".json");
    public string CvFolder(string profile, string field) => Path.Combine(FieldFolder(profile, field), "CVs");
    private string CvPath(string profile, string field, string role) =>
        Path.Combine(CvFolder(profile, field), NameCleaner.Clean(role) + ".json");

    // PDF and Word folder
    public string CvOutputRoot { get; set; } = "";

    // Where PDF and Word go
    public string OutputFolder(string profile, string field) => CvOutputRoot == ""
        ? CvFolder(profile, field)
        : Path.Combine(CvOutputRoot, NameCleaner.Clean(profile), NameCleaner.Clean(field));

    // Output folder, else data folder
    public string FolderToShow(string profile, string field) =>
        Directory.Exists(OutputFolder(profile, field)) ? OutputFolder(profile, field) : CvFolder(profile, field);

    // Profile's top CV folder
    public string OutputRoot(string profile) => CvOutputRoot == ""
        ? FieldsFolder(profile)
        : Path.Combine(CvOutputRoot, NameCleaner.Clean(profile));

    // Sent CV copies
    public string ApplicationsFolder(string profile) => CvOutputRoot == ""
        ? Path.Combine(ProfileFolder(profile), "Applications")
        : Path.Combine(CvOutputRoot, NameCleaner.Clean(profile), "Applications");

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
    public void SavePicture(string profile, System.Windows.Media.Imaging.BitmapSource picture)
    {
        // Failed save keeps old picture
        string temp = PicturePath(profile) + ".new";
        PictureFile.SaveJpeg(picture, temp);
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

    public List<string> ListSpecialties(string profile, string field) => ListJsonNames(SpecialtiesFolder(profile, field));

    public void CreateField(string profile, string field) => Directory.CreateDirectory(FieldFolder(profile, field));

    public void RenameField(string profile, string field, string newName) =>
        Directory.Move(FieldFolder(profile, field), FieldFolder(profile, newName));

    // Specialties only; CVs stay
    public void DeleteField(string profile, string field)
    {
        string specialties = SpecialtiesFolder(profile, field);
        if (Directory.Exists(specialties))
            Directory.Delete(specialties, recursive: true);
        string folder = FieldFolder(profile, field);
        if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
            Directory.Delete(folder);
    }

    public bool SpecialtyExists(string profile, string field, string specialty) =>
        File.Exists(SpecialtyPath(profile, field, specialty));

    public CvDocument LoadSpecialty(string profile, string field, string specialty) =>
        WithLanguage(Read<CvDocument>(SpecialtyPath(profile, field, specialty)));

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
        foreach (string field in ListFields(profile))
        {
            string folder = CvFolder(profile, field);
            if (!Directory.Exists(folder))
                continue;
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                var item = new CvListItem
                {
                    Field = field,
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

    public bool CvExists(string profile, string field, string role) => File.Exists(CvPath(profile, field, role));

    public CvDocument LoadCv(string profile, string field, string role) => WithLanguage(Read<CvDocument>(CvPath(profile, field, role)));

    // Old files: guess the language
    private static CvDocument WithLanguage(CvDocument doc)
    {
        if (doc.Language == "")
            doc.Language = CvDocument.GuessLanguage(doc, Text.Language);
        return doc;
    }

    public void SaveCv(string profile, string field, string role, CvDocument doc) => Write(CvPath(profile, field, role), doc);

    // Newest PDF made for a CV
    public string? LatestPdf(string profile, string field, string role, string fullName)
    {
        string folder = OutputFolder(profile, field);
        if (!Directory.Exists(folder))
            return null;
        var pattern = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(NameCleaner.Clean($"{fullName} - {role}")) + @"(_Pic)?(_\d+)?\.pdf$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return Directory.GetFiles(folder, "*.pdf")
            .Where(file => pattern.IsMatch(Path.GetFileName(file)))
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    // ---- Old layout ----

    // Old folders to new layout
    public void Migrate(string profile)
    {
        // Freeze sent PDFs first
        new ApplicationStore(this).FreezeOldRows(profile);

        // Specialties into their subfolder
        foreach (string field in ListFields(profile))
        {
            foreach (string file in Directory.GetFiles(FieldFolder(profile, field), "*.json"))
            {
                Directory.CreateDirectory(SpecialtiesFolder(profile, field));
                File.Move(file, FreeName(Path.Combine(SpecialtiesFolder(profile, field), Path.GetFileName(file))));
            }
        }

        // Company CVs into their Field
        string oldCvs = Path.Combine(ProfileFolder(profile), "CVs");
        foreach (string company in ListFolderNames(oldCvs))
        {
            string companyFolder = Path.Combine(oldCvs, company);
            var movedTo = new List<(string Role, string Folder)>();
            foreach (string file in Directory.GetFiles(companyFolder, "*.json"))
            {
                string field = "General";
                try
                {
                    string saved = Read<CvDocument>(file).Field;
                    if (saved != "")
                        field = saved;
                }
                catch (Exception ex) when (ex is JsonException or InvalidDataException)
                {
                }
                string target = CvFolder(profile, field);
                Directory.CreateDirectory(target);
                File.Move(file, FreeName(Path.Combine(target, Path.GetFileName(file))));
                movedTo.Add((Path.GetFileNameWithoutExtension(file), target));
            }

            // PDF and Word follow role
            foreach (string output in Directory.GetFiles(companyFolder))
            {
                if (movedTo.Count == 0)
                    break;
                string name = Path.GetFileNameWithoutExtension(output);
                var owner = movedTo.FirstOrDefault(m => name.Contains(" - " + m.Role, StringComparison.OrdinalIgnoreCase));
                string folder = owner.Folder ?? movedTo[0].Folder;
                File.Move(output, FreeName(Path.Combine(folder, Path.GetFileName(output))));
            }
            if (!Directory.EnumerateFileSystemEntries(companyFolder).Any())
                Directory.Delete(companyFolder);
        }
        if (Directory.Exists(oldCvs) && !Directory.EnumerateFileSystemEntries(oldCvs).Any())
            Directory.Delete(oldCvs);
    }

    // Taken name gets _2, _3
    public static string FreeName(string path)
    {
        string folder = Path.GetDirectoryName(path)!;
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        for (int number = 2; File.Exists(path); number++)
            path = Path.Combine(folder, $"{name}_{number}{extension}");
        return path;
    }

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
