using System.IO;
using System.Windows;
using FastVitae.Model;
using FastVitae.Storage;
using FastVitae.Views;

namespace FastVitae;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    // Found once, at app open
    public static Release? AvailableUpdate { get; private set; }
    public static bool BubbleShown { get; set; }
    public static bool Updating { get; set; }
    public static event Action? UpdateFound;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Old exe may still be closing
        Updater.CleanOld(Environment.ProcessPath!);
        _ = Task.Delay(5000).ContinueWith(_ => Updater.CleanOld(Environment.ProcessPath!));

        // Stay alive between windows
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Settings = AppSettings.Load();
        Text.SetLanguage(Settings.Language != "" ? Settings.Language : Text.SystemLanguage());
        string folder = Settings.DataFolder;
        string reason = folder != "" && !Directory.Exists(folder) ? Text.Format("The data folder was not found: {0}", folder) : "";
        var info = new BasicInfo();

        // Ask until a profile exists
        while (folder == "" || !Directory.Exists(folder) || new ProfileStore(folder).ListProfiles().Count == 0)
        {
            var setup = new SetupWindow(info, folder == "" ? AppSettings.DefaultDataFolder : folder, reason);
            if (setup.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            try
            {
                Directory.CreateDirectory(setup.DataFolder);
                var newStore = new ProfileStore(setup.DataFolder);
                Settings.LastProfile = newStore.SaveProfile(info);
                setup.SavePictureChoice(newStore, Settings.LastProfile);
                Settings.SetDataFolder(setup.DataFolder);
                Settings.Language = Text.Language;
                Settings.Save();
                folder = Settings.DataFolder;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                reason = Text.Get("That folder could not be used:") + " " + ex.Message;
            }
        }

        var profiles = new ProfileStore(folder).ListProfiles();
        string profile = profiles.Contains(Settings.LastProfile) ? Settings.LastProfile : profiles[0];
        ShowHome(profile);
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        if (Settings.CheckForUpdates)
            _ = LookForUpdate();
    }

    // One check, then tell Home
    private static async Task LookForUpdate()
    {
        var release = await Updater.CheckAsync();
        if (release == null)
            return;
        AvailableUpdate = release;
        UpdateFound?.Invoke();
    }

    // Open Home with current settings
    public static void ShowHome(string profile)
    {
        var store = new ProfileStore(Settings.DataFolder) { CvOutputRoot = Settings.CvOutputFolder };
        var old = Current.MainWindow;
        var home = new HomeWindow(store, profile);
        Current.MainWindow = home;
        home.Show();
        if (old != null && old != home && old.IsLoaded)
            old.Close();
    }

    // Save settings, report failure
    public static bool SaveSettings(Window owner)
    {
        try
        {
            Settings.Save();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(owner, Text.Get("The settings could not be saved:") + " " + ex.Message, "Fast Vitae");
            return false;
        }
    }

    // Pick and save picture
    public static bool ChoosePicture(Window owner, ProfileStore store, string profile)
    {
        string name = profile, title = "";
        try
        {
            var info = store.LoadProfile(profile);
            name = info.FullName;
            title = info.Title;
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
        }

        var picture = PickPicture(owner, name, title);
        if (picture == null)
            return false;

        try
        {
            store.SavePicture(profile, picture);
            return true;
        }
        catch (Exception ex) when (ex is NotSupportedException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(owner, Text.Get("The picture could not be saved:") + " " + ex.Message, "Fast Vitae");
            return false;
        }
    }

    // Pick a file, then crop
    public static System.Windows.Media.Imaging.BitmapSource? PickPicture(Window owner, string name, string title)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Text.Get("Choose your picture"),
            Filter = Text.Get("Pictures") + "|*.jpg;*.jpeg;*.png;*.bmp|" + Text.Get("All files") + "|*.*"
        };
        if (dialog.ShowDialog(owner) != true)
            return null;

        System.Windows.Media.Imaging.BitmapSource picture;
        try
        {
            picture = PictureFile.LoadForCrop(dialog.FileName);
        }
        catch (Exception ex) when (ex is NotSupportedException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(owner, Text.Get("That file could not be opened as a picture."), "Fast Vitae");
            return null;
        }

        var crop = new CropWindow(picture, name, title) { Owner = owner };
        return crop.ShowDialog() == true ? crop.Cropped : null;
    }

    // Show a folder in Explorer
    public static void OpenFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            MessageBox.Show(Text.Get("The folder was not found:") + "\n" + folder, "Fast Vitae");
            return;
        }
        System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\"");
    }

    // Remember the last profile
    public static void RememberProfile(string profile)
    {
        Settings.LastProfile = profile;
        try
        {
            Settings.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
