using System.IO;
using System.Windows;
using FastVitae.Model;
using FastVitae.Storage;
using FastVitae.Views;

namespace FastVitae;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Stay alive between windows
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Settings = AppSettings.Load();
        string folder = Settings.FullDataFolder;
        string reason = folder != "" && !Directory.Exists(folder) ? $"The data folder was not found: {folder}" : "";
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
                Settings.Save();
                folder = Settings.FullDataFolder;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                reason = "That folder could not be used: " + ex.Message;
            }
        }

        var store = new ProfileStore(folder);
        var profiles = store.ListProfiles();
        string profile = profiles.Contains(Settings.LastProfile) ? Settings.LastProfile : profiles[0];

        var home = new HomeWindow(store, profile);
        MainWindow = home;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        home.Show();
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
