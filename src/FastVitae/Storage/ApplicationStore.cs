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

    public string StatusColor => ApplicationStatus.ColorOf(Status);
}

// Applications in one Excel per Field
public class ApplicationStore
{
    private const int CompanyColumn = 1;
    private const int RoleColumn = 2;
    private const int DateColumn = 3;
    private const int CvColumn = 4;
    private const int StatusColumn = 5;

    private readonly ProfileStore store;

    public ApplicationStore(ProfileStore store)
    {
        this.store = store;
    }

    private string Folder(string profile) => Path.Combine(store.ProfileFolder(profile), "Applications");

    public string FilePath(string profile, string field) =>
        Path.Combine(Folder(profile), NameCleaner.Clean(field) + ".xlsx");

    // Read every Field's file
    public List<JobApplication> List(string profile)
    {
        var applications = new List<JobApplication>();
        if (!Directory.Exists(Folder(profile)))
            return applications;

        foreach (string file in Directory.GetFiles(Folder(profile), "*.xlsx"))
        {
            // Skip Excel's lock files
            if (Path.GetFileName(file).StartsWith("~$"))
                continue;

            // Readable while open in Excel
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var book = new XLWorkbook(stream);
            var sheet = book.Worksheets.First();

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                string status = row.Cell(StatusColumn).GetString().Trim();
                row.Cell(DateColumn).TryGetValue(out DateTime date);
                applications.Add(new JobApplication
                {
                    Field = Path.GetFileNameWithoutExtension(file),
                    Company = row.Cell(CompanyColumn).GetString(),
                    Role = row.Cell(RoleColumn).GetString(),
                    DateApplied = date,
                    CvPath = row.Cell(CvColumn).GetString(),
                    Status = ApplicationStatus.All.Contains(status) ? status : ApplicationStatus.Applied
                });
            }
        }
        return applications.OrderByDescending(a => a.DateApplied).ToList();
    }

    // Add or update on Create
    public void Record(string profile, string field, string company, string role, string cvPath)
    {
        string path = FilePath(profile, field);
        using var book = OpenOrCreate(path);
        var sheet = book.Worksheets.First();

        var row = FindRow(sheet, company, role);
        if (row == null)
        {
            row = sheet.Row((sheet.LastRowUsed()?.RowNumber() ?? 1) + 1);
            row.Cell(CompanyColumn).Value = company;
            row.Cell(RoleColumn).Value = role;
            row.Cell(DateColumn).Value = DateTime.Today;
            row.Cell(StatusColumn).Value = ApplicationStatus.Applied;
            Paint(row, ApplicationStatus.Applied);
        }
        row.Cell(CvColumn).Value = cvPath;

        book.SaveAs(path);
    }

    // Change status and row colour
    public void SetStatus(string profile, JobApplication application, string status)
    {
        string path = FilePath(profile, application.Field);
        using var book = new XLWorkbook(path);
        var row = FindRow(book.Worksheets.First(), application.Company, application.Role);
        if (row == null)
            throw new IOException("That application is no longer in the Excel file.");

        row.Cell(StatusColumn).Value = status;
        Paint(row, status);
        book.Save();
    }

    private static IXLRow? FindRow(IXLWorksheet sheet, string company, string role) =>
        sheet.RowsUsed().Skip(1).FirstOrDefault(row =>
            row.Cell(CompanyColumn).GetString() == company && row.Cell(RoleColumn).GetString() == role);

    private static void Paint(IXLRow row, string status) =>
        row.Cells(CompanyColumn, StatusColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(ApplicationStatus.ColorOf(status));

    // New file gets a header
    private static XLWorkbook OpenOrCreate(string path)
    {
        if (File.Exists(path))
            return new XLWorkbook(path);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var book = new XLWorkbook();
        var sheet = book.AddWorksheet("Applications");
        sheet.Cell(1, CompanyColumn).Value = "Company";
        sheet.Cell(1, RoleColumn).Value = "Role";
        sheet.Cell(1, DateColumn).Value = "Date applied";
        sheet.Cell(1, CvColumn).Value = "CV";
        sheet.Cell(1, StatusColumn).Value = "Status";
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(CompanyColumn).Width = 28;
        sheet.Column(RoleColumn).Width = 28;
        sheet.Column(DateColumn).Width = 14;
        sheet.Column(CvColumn).Width = 60;
        sheet.Column(StatusColumn).Width = 14;
        return book;
    }
}
