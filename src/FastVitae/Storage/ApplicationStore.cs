using System.IO;
using ClosedXML.Excel;

namespace FastVitae.Storage;

// Status names and colours
public static class ApplicationStatus
{
    public const string Applied = "Applied";
    public const string InProcess = "In process";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = { Applied, InProcess, Approved, Rejected };

    public static string ColorOf(string status) => status switch
    {
        InProcess => "#DDEBF7",
        Approved => "#D9EAD3",
        Rejected => "#F4CCCC",
        _ => "#FFF2CC"
    };

    // App row colour
    public static string RowColorOf(string status) => status switch
    {
        InProcess => "#3A6A92",
        Approved => "#3F7A4C",
        Rejected => "#8F4040",
        _ => "#8A7A33"
    };

    // App row colour on hover
    public static string DarkColorOf(string status) => status switch
    {
        InProcess => "#1E3F5A",
        Approved => "#24472B",
        Rejected => "#5A2626",
        _ => "#574B1C"
    };
}

// One job application row
public class JobApplication
{
    public string Field { get; set; } = "";
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime DateApplied { get; set; }
    public string CvPath { get; set; } = "";
    public string Status { get; set; } = ApplicationStatus.Applied;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "";
    public string Link { get; set; } = "";

    // "2,000 - 2,500 USD"
    public string SalaryTextIn(IFormatProvider culture)
    {
        var amounts = new[] { SalaryMin, SalaryMax }.Where(a => a != null).Select(a => a!.Value.ToString("#,0.##", culture)).ToList();
        return amounts.Count == 0 ? "" : $"{string.Join(" - ", amounts)} {Currency}".Trim();
    }

    public string SalaryText => SalaryTextIn(System.Globalization.CultureInfo.CurrentCulture);

    public string StatusColor => ApplicationStatus.RowColorOf(Status);
    public string StatusHoverColor => ApplicationStatus.DarkColorOf(Status);
}

// Applications Excel, one per profile
public class ApplicationStore
{
    private const int FieldColumn = 1;
    private const int CompanyColumn = 2;
    private const int RoleColumn = 3;
    private const int DateColumn = 4;
    private const int CvColumn = 5;
    private const int StatusColumn = 6;
    private const int SalaryMinColumn = 7;
    private const int SalaryMaxColumn = 8;
    private const int CurrencyColumn = 9;
    private const int LinkColumn = 10;

    private readonly ProfileStore store;

    public ApplicationStore(ProfileStore store)
    {
        this.store = store;
    }

    public string FilePath(string profile) => Path.Combine(store.ProfileFolder(profile), "Applications.xlsx");

    // Old per-Field files
    private string OldFolder(string profile) => Path.Combine(store.ProfileFolder(profile), "Applications");

    public List<JobApplication> List(string profile)
    {
        MergeOldFiles(profile);
        var applications = new List<JobApplication>();
        string path = FilePath(profile);
        if (!File.Exists(path))
            return applications;

        // Readable while open in Excel
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var book = new XLWorkbook(stream);
        foreach (var row in book.Worksheets.First().RowsUsed().Skip(1))
        {
            string status = row.Cell(StatusColumn).GetString().Trim();
            row.Cell(DateColumn).TryGetValue(out DateTime date);
            applications.Add(new JobApplication
            {
                Field = row.Cell(FieldColumn).GetString(),
                Company = row.Cell(CompanyColumn).GetString(),
                Role = row.Cell(RoleColumn).GetString(),
                DateApplied = date,
                CvPath = row.Cell(CvColumn).GetString(),
                Status = ApplicationStatus.All.Contains(status) ? status : ApplicationStatus.Applied,
                SalaryMin = row.Cell(SalaryMinColumn).TryGetValue(out decimal min) && !row.Cell(SalaryMinColumn).IsEmpty() ? min : null,
                SalaryMax = row.Cell(SalaryMaxColumn).TryGetValue(out decimal max) && !row.Cell(SalaryMaxColumn).IsEmpty() ? max : null,
                Currency = row.Cell(CurrencyColumn).GetString(),
                Link = row.Cell(LinkColumn).GetString()
            });
        }
        return applications.OrderByDescending(a => a.DateApplied).ToList();
    }

    // New row with frozen CV copy
    public void Add(string profile, JobApplication application, string? sourcePdf)
    {
        MergeOldFiles(profile);
        if (sourcePdf != null)
            application.CvPath = FreezeCopy(profile, application, sourcePdf);

        string path = FilePath(profile);
        using var book = OpenOrCreate(path);
        var sheet = book.Worksheets.First();
        AddHeaders(sheet);
        var row = AddRow(sheet, application.Field, application.Company, application.Role, application.DateApplied, application.Status);
        row.Cell(CvColumn).Value = application.CvPath;
        if (application.SalaryMin != null)
            row.Cell(SalaryMinColumn).Value = application.SalaryMin.Value;
        if (application.SalaryMax != null)
            row.Cell(SalaryMaxColumn).Value = application.SalaryMax.Value;
        row.Cell(CurrencyColumn).Value = application.Currency;
        row.Cell(LinkColumn).Value = application.Link;
        book.SaveAs(path);
    }

    // Copy sent PDF, return relative path
    private string FreezeCopy(string profile, JobApplication application, string sourcePdf)
    {
        string folder = store.ApplicationsFolder(profile);
        Directory.CreateDirectory(folder);
        string name = NameCleaner.Clean($"{application.Company} - {application.Role} - {application.DateApplied:yyyy-MM-dd}");
        string target = ProfileStore.FreeName(Path.Combine(folder, name + ".pdf"));
        File.Copy(sourcePdf, target);
        return Path.GetRelativePath(store.ProfileFolder(profile), target);
    }

    // Old rows get frozen copies
    public void FreezeOldRows(string profile)
    {
        MergeOldFiles(profile);
        string path = FilePath(profile);
        if (!File.Exists(path))
            return;

        using var book = new XLWorkbook(path);
        bool changed = false;
        string profileFolder = store.ProfileFolder(profile);
        foreach (var row in book.Worksheets.First().RowsUsed().Skip(1))
        {
            string cvPath = row.Cell(CvColumn).GetString();
            if (cvPath == "")
                continue;
            string full = Path.GetFullPath(cvPath, profileFolder);
            bool alreadyFrozen = full.StartsWith(store.ApplicationsFolder(profile) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (alreadyFrozen || !File.Exists(full))
                continue;

            row.Cell(DateColumn).TryGetValue(out DateTime date);
            var application = new JobApplication { Company = row.Cell(CompanyColumn).GetString(), Role = row.Cell(RoleColumn).GetString(), DateApplied = date };
            row.Cell(CvColumn).Value = FreezeCopy(profile, application, full);
            changed = true;
        }
        if (changed)
            book.Save();
    }

    // Change status and row colour
    public void SetStatus(string profile, JobApplication application, string status)
    {
        string path = FilePath(profile);
        using var book = new XLWorkbook(path);
        var row = FindRow(book.Worksheets.First(), application);
        if (row == null)
            throw new IOException("That application is no longer in the Excel file.");

        row.Cell(StatusColumn).Value = status;
        Paint(row, status);
        book.Save();
    }

    // Move old Field files in
    private void MergeOldFiles(string profile)
    {
        string oldFolder = OldFolder(profile);
        if (!Directory.Exists(oldFolder))
            return;
        var oldFiles = Directory.GetFiles(oldFolder, "*.xlsx").Where(file => !Path.GetFileName(file).StartsWith("~$")).ToList();
        if (oldFiles.Count == 0)
            return;

        string path = FilePath(profile);
        using (var book = OpenOrCreate(path))
        {
            var sheet = book.Worksheets.First();
            foreach (string file in oldFiles)
            {
                string field = Path.GetFileNameWithoutExtension(file);
                using var oldBook = new XLWorkbook(file);
                // Old column order
                foreach (var old in oldBook.Worksheets.First().RowsUsed().Skip(1))
                {
                    old.Cell(3).TryGetValue(out DateTime date);
                    var row = AddRow(sheet, field, old.Cell(1).GetString(), old.Cell(2).GetString(), date, old.Cell(5).GetString().Trim());
                    row.Cell(CvColumn).Value = old.Cell(4).GetString();
                }
            }
            book.SaveAs(path);
        }

        // Old Excel files aside only
        string aside = oldFolder + " (old)";
        Directory.CreateDirectory(aside);
        foreach (string file in oldFiles)
            File.Move(file, ProfileStore.FreeName(Path.Combine(aside, Path.GetFileName(file))));
    }

    private static IXLRow AddRow(IXLWorksheet sheet, string field, string company, string role, DateTime date, string status)
    {
        if (!ApplicationStatus.All.Contains(status))
            status = ApplicationStatus.Applied;
        var row = sheet.Row((sheet.LastRowUsed()?.RowNumber() ?? 1) + 1);
        row.Cell(FieldColumn).Value = field;
        row.Cell(CompanyColumn).Value = company;
        row.Cell(RoleColumn).Value = role;
        row.Cell(DateColumn).Value = date;
        row.Cell(StatusColumn).Value = status;
        Paint(row, status);
        return row;
    }

    // Frozen copy is unique
    private static IXLRow? FindRow(IXLWorksheet sheet, JobApplication application) =>
        sheet.RowsUsed().Skip(1).FirstOrDefault(row =>
            row.Cell(CvColumn).GetString() == application.CvPath &&
            row.Cell(FieldColumn).GetString() == application.Field &&
            row.Cell(CompanyColumn).GetString() == application.Company &&
            row.Cell(RoleColumn).GetString() == application.Role);

    // Old files lack new columns
    private static void AddHeaders(IXLWorksheet sheet)
    {
        if (!sheet.Cell(1, SalaryMinColumn).IsEmpty())
            return;
        sheet.Cell(1, SalaryMinColumn).Value = Text.Get("Salary from");
        sheet.Cell(1, SalaryMaxColumn).Value = Text.Get("Salary to");
        sheet.Cell(1, CurrencyColumn).Value = Text.Get("Currency");
        sheet.Cell(1, LinkColumn).Value = Text.Get("Link");
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(SalaryMinColumn).Width = 12;
        sheet.Column(SalaryMaxColumn).Width = 12;
        sheet.Column(CurrencyColumn).Width = 10;
        sheet.Column(LinkColumn).Width = 50;
    }

    private static void Paint(IXLRow row, string status) =>
        row.Cells(FieldColumn, StatusColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(ApplicationStatus.ColorOf(status));

    // New file gets a header
    private static XLWorkbook OpenOrCreate(string path)
    {
        if (File.Exists(path))
            return new XLWorkbook(path);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var book = new XLWorkbook();
        var sheet = book.AddWorksheet("Applications");
        sheet.Cell(1, FieldColumn).Value = Text.Get("Field");
        sheet.Cell(1, CompanyColumn).Value = Text.Get("Company");
        sheet.Cell(1, RoleColumn).Value = Text.Get("Role");
        sheet.Cell(1, DateColumn).Value = Text.Get("Date applied");
        sheet.Cell(1, CvColumn).Value = "CV";
        sheet.Cell(1, StatusColumn).Value = Text.Get("Status");
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(FieldColumn).Width = 22;
        sheet.Column(CompanyColumn).Width = 28;
        sheet.Column(RoleColumn).Width = 28;
        sheet.Column(DateColumn).Width = 14;
        sheet.Column(CvColumn).Width = 60;
        sheet.Column(StatusColumn).Width = 14;
        return book;
    }
}
