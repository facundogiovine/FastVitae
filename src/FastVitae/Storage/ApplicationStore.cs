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
                Status = ApplicationStatus.All.Contains(status) ? status : ApplicationStatus.Applied
            });
        }
        return applications.OrderByDescending(a => a.DateApplied).ToList();
    }

    // Add or update on Create
    public void Record(string profile, string field, string company, string role, string cvPath)
    {
        MergeOldFiles(profile);
        string path = FilePath(profile);
        using var book = OpenOrCreate(path);
        var row = FindRow(book.Worksheets.First(), field, company, role) ?? AddRow(book.Worksheets.First(), field, company, role, DateTime.Today, ApplicationStatus.Applied);
        row.Cell(CvColumn).Value = cvPath;
        book.SaveAs(path);
    }

    // Change status and row colour
    public void SetStatus(string profile, JobApplication application, string status)
    {
        string path = FilePath(profile);
        using var book = new XLWorkbook(path);
        var row = FindRow(book.Worksheets.First(), application.Field, application.Company, application.Role);
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

        // Keep the old files aside
        string aside = oldFolder + " (old)";
        Directory.Move(oldFolder, Directory.Exists(aside) ? aside + " " + DateTime.Now.ToString("yyyyMMddHHmmss") : aside);
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

    private static IXLRow? FindRow(IXLWorksheet sheet, string field, string company, string role) =>
        sheet.RowsUsed().Skip(1).FirstOrDefault(row =>
            row.Cell(FieldColumn).GetString() == field &&
            row.Cell(CompanyColumn).GetString() == company &&
            row.Cell(RoleColumn).GetString() == role);

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
