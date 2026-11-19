using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace FastVitae.Views;

// Move data or CV output
public partial class SaveLocationWindow : Window
{
    public SaveLocationWindow()
    {
        InitializeComponent();
        bool onlyCvs = App.Settings.CvOutputFolder != "";
        FolderBox.Text = onlyCvs ? App.Settings.CvOutputFolder : App.Settings.DataFolder;
        OnlyNewCvs.IsChecked = onlyCvs;
        ShowNote();
    }

    private void OnlyNewCvs_Click(object sender, RoutedEventArgs e) => ShowNote();

    // What happens to existing CVs
    private void ShowNote() => MoveNote.Text = OnlyNewCvs.IsChecked == true
        ? Text.Get("The existing Fast Vitae CV folders will stay in the old folder.")
        : Text.Get("The Fast Vitae CV folders will be moved to the new folder.");

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Text.Get("Change where CVs are saved") };
        if (dialog.ShowDialog(this) == true)
            FolderBox.Text = dialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string folder = FolderBox.Text.Trim();
        if (folder == "")
        {
            MessageBox.Show(this, Text.Get("Please choose a folder."), "Fast Vitae");
            return;
        }

        try
        {
            folder = Path.GetFullPath(folder);
            if (OnlyNewCvs.IsChecked == true)
            {
                // New PDFs and Words only
                Directory.CreateDirectory(folder);
                App.Settings.CvOutputFolder = folder;
            }
            else
            {
                MoveData(folder);
                App.Settings.CvOutputFolder = "";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, Text.Get("The data could not be moved:") + " " + ex.Message, "Fast Vitae");
            return;
        }

        if (App.SaveSettings(this))
            DialogResult = true;
    }

    // Move everything to the folder
    private static void MoveData(string folder)
    {
        string current = Path.GetFullPath(App.Settings.DataFolder).TrimEnd('\\');
        if (string.Equals(folder.TrimEnd('\\'), current, StringComparison.OrdinalIgnoreCase))
            return;

        // Busy folder gets a subfolder
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            folder = Path.Combine(folder, "Fast Vitae");
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new IOException(Text.Get("The folder is not empty:") + " " + folder);
        if (folder.StartsWith(current + "\\", StringComparison.OrdinalIgnoreCase))
            throw new IOException(Text.Get("The new folder cannot be inside the current one."));

        if (Directory.Exists(folder))
            Directory.Delete(folder);
        Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(current, folder);
        App.Settings.SetDataFolder(folder);
    }
}
