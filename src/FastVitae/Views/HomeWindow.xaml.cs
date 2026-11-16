using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FastVitae.Model;
using FastVitae.Storage;

namespace FastVitae.Views;

// CV list grouped by company
public partial class HomeWindow : Window
{
    private readonly ProfileStore store;
    private string profile;

    public HomeWindow(ProfileStore store, string profile)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;
        LanguageBox.SelectedValue = Text.Language;
        Refresh();

        // Update button and bubble
        Loaded += (s, e) => ShowUpdate();
        App.UpdateFound += ShowUpdate;
        Closed += (s, e) => App.UpdateFound -= ShowUpdate;
        LocationChanged += (s, e) => UpdateBubble.IsOpen = false;
        Deactivated += (s, e) => UpdateBubble.IsOpen = false;
    }

    // ---- Update ----

    private void ShowUpdate()
    {
        if (App.AvailableUpdate == null || !IsLoaded)
            return;
        UpdateButton.Visibility = Visibility.Visible;
        if (App.Updating)
        {
            UpdateButton.IsEnabled = false;
            UpdateButton.Content = Text.Get("Updating...");
        }
        if (App.BubbleShown)
            return;
        App.BubbleShown = true;
        UpdateBubble.IsOpen = true;
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            UpdateBubble.IsOpen = false;
        };
        timer.Start();
    }

    // Download, swap, restart
    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        var release = App.AvailableUpdate;
        if (release == null || App.Updating)
            return;
        App.Updating = true;
        UpdateBubble.IsOpen = false;
        UpdateButton.IsEnabled = false;
        UpdateButton.Content = Text.Get("Updating...");
        string exePath = Environment.ProcessPath!;
        string newPath = exePath + ".new";
        try
        {
            await Updater.DownloadAsync(release, newPath);

            // Other windows may ask first
            if (!CloseOthers(this, Application.Current.Windows.OfType<Window>()))
            {
                TryDelete(newPath);
                RestoreUpdateButton();
                return;
            }

            Updater.Swap(exePath, newPath);
        }
        catch (Exception ex) when (Updater.IsNetworkError(ex))
        {
            TryDelete(newPath);
            ShowUpdateError(ex);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Folder not writable: browser
            TryDelete(newPath);
            RestoreUpdateButton();
            OpenInBrowser(release.PageUrl);
            return;
        }

        try
        {
            Process.Start(exePath);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            // New exe blocked: restore old
            try
            {
                Updater.Undo(exePath);
            }
            catch (Exception ex2) when (ex2 is IOException or UnauthorizedAccessException)
            {
            }
            ShowUpdateError(ex);
        }
    }

    private void ShowUpdateError(Exception ex)
    {
        MessageBox.Show(this, Text.Get("The update could not be installed:") + " " + ex.Message, "Fast Vitae");
        RestoreUpdateButton();
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    // Close others; false if refused
    public static bool CloseOthers(Window keep, IEnumerable<Window> windows)
    {
        var others = windows.Where(w => w != keep).Reverse().ToList();
        var closed = new HashSet<Window>();
        foreach (var window in others)
            window.Closed += (s, e) => closed.Add(window);

        // Newest first, stop on refusal
        foreach (var window in others)
        {
            if (closed.Contains(window))
                continue;
            window.Close();
            if (!closed.Contains(window))
                return false;
        }
        return true;
    }

    private void RestoreUpdateButton()
    {
        App.Updating = false;
        UpdateButton.IsEnabled = true;
        UpdateButton.Content = Text.Get("Update");
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void UpdateBubble_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => UpdateBubble.IsOpen = false;

    private void Refresh()
    {
        Title = "Fast Vitae - " + profile;
        var items = store.ListCvs(profile);
        var view = new ListCollectionView(items);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CvListItem.Company)));
        CvList.ItemsSource = view;
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshApplications();
    }

    // ---- Applications tab ----

    private static string AllFields => Text.Get("All fields");
    private static string AllStatuses => Text.Get("All statuses");
    private List<JobApplication> applications = new();
    private bool fillingFilters;

    // Reload from the Excel files
    private void RefreshApplications()
    {
        try
        {
            applications = new ApplicationStore(store).List(profile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            applications = new List<JobApplication>();
            MessageBox.Show(this, Text.Get("The applications could not be read:") + " " + ex.Message, "Fast Vitae");
        }

        // Keep the chosen filters
        string field = FieldFilter.SelectedItem as string ?? AllFields;
        string status = StatusFilter.SelectedItem as string ?? AllStatuses;
        fillingFilters = true;
        FieldFilter.ItemsSource = new[] { AllFields }.Concat(applications.Select(a => a.Field).Distinct().OrderBy(f => f)).ToList();
        FieldFilter.SelectedItem = FieldFilter.Items.Contains(field) ? field : AllFields;
        StatusFilter.ItemsSource = new[] { AllStatuses }.Concat(ApplicationStatus.All).ToList();
        StatusFilter.SelectedItem = status;
        fillingFilters = false;

        ShowApplications();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!fillingFilters)
            ShowApplications();
    }

    // Apply the two filters
    private void ShowApplications()
    {
        string field = FieldFilter.SelectedItem as string ?? AllFields;
        string status = StatusFilter.SelectedItem as string ?? AllStatuses;

        ApplicationList.ItemsSource = applications
            .Where(a => field == AllFields || a.Field == field)
            .Where(a => status == AllStatuses || a.Status == status)
            .ToList();
        NoApplicationsText.Visibility = applications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Save status to the Excel
    private void Status_Changed(object sender, SelectionChangedEventArgs e)
    {
        var combo = (ComboBox)sender;
        if (combo.DataContext is not JobApplication application || combo.SelectedItem is not string status)
            return;
        if (status == application.Status)
            return;

        try
        {
            new ApplicationStore(store).SetStatus(profile, application, status);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                Text.Get("The status could not be saved. If the Excel file is open, close it and try again.") + "\n\n" + ex.Message,
                "Fast Vitae", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        RefreshApplications();
    }

    // Open the PDF applied with
    private void OpenCv_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not JobApplication application)
            return;

        string path = Path.Combine(store.ProfileFolder(profile), application.CvPath);
        if (!File.Exists(path))
        {
            MessageBox.Show(this, Text.Get("The CV file was not found:") + "\n" + path, "Fast Vitae");
            return;
        }
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    // Explorer with the PDF selected
    private void ShowCvFile_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not JobApplication application)
            return;

        string path = Path.GetFullPath(Path.Combine(store.ProfileFolder(profile), application.CvPath));
        if (!File.Exists(path))
        {
            MessageBox.Show(this, Text.Get("The CV file was not found:") + "\n" + path, "Fast Vitae");
            return;
        }
        Process.Start("explorer.exe", "/select,\"" + path + "\"");
    }

    // ---- CVs tab and toolbar ----

    private void NewCv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewCvWindow(store, profile) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        var editor = EditorWindow.ForCv(store, profile, dialog.Document!, dialog.Company, dialog.Role, dialog.AlreadyExists);
        editor.Owner = this;
        editor.ShowDialog();
        Refresh();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (CvList.SelectedItem is CvListItem item)
            OpenCv(item);
    }

    // Pencil on the clicked row
    private void EditCv_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is CvListItem item)
            OpenCv(item);
    }

    private void OpenCv(CvListItem item)
    {
        if (item.Unreadable)
        {
            MessageBox.Show(this, Text.Get("This file could not be read, so it was left untouched:") + "\n" + item.Path, "Fast Vitae");
            return;
        }

        var doc = store.LoadCv(profile, item.Company, item.Role);
        var editor = EditorWindow.ForCv(store, profile, doc, item.Company, item.Role, alreadyCreated: true);
        editor.Owner = this;
        editor.ShowDialog();
        Refresh();
    }

    // Folder of the clicked row
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is CvListItem item)
            App.OpenFolder(store.FolderToShow(profile, item.Company));
    }

    private void OpenCvsFolder_Click(object sender, RoutedEventArgs e) => App.OpenFolder(store.OutputRoot(profile));

    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || LanguageBox.SelectedValue is not string code || code == Text.Language)
            return;
        Text.SetLanguage(code);
        App.Settings.Language = code;
        App.SaveSettings(this);
        Refresh();
    }

    private void SaveLocation_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveLocationWindow { Owner = this };
        if (dialog.ShowDialog() == true)
            App.ShowHome(profile);
    }

    private void Fields_Click(object sender, RoutedEventArgs e)
    {
        new FieldsWindow(store, profile) { Owner = this }.ShowDialog();
    }

    // Edit the current profile
    private void Profile_Click(object sender, RoutedEventArgs e)
    {
        var info = store.LoadProfile(profile);
        var dialog = new SetupWindow(info, dataFolder: null, currentPicture: store.PicturePath(profile))
        {
            Owner = this,
            Title = Text.Get("Fast Vitae - Profile")
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            // New name moves the folder
            string newName = NameCleaner.Clean(info.FullName);
            if (!newName.Equals(profile, StringComparison.OrdinalIgnoreCase))
            {
                if (store.ListProfiles().Contains(newName, StringComparer.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, Text.Format("There is already a profile called {0}.", newName), "Fast Vitae");
                    return;
                }
                store.RenameProfile(profile, newName);
            }
            profile = store.SaveProfile(info);
            dialog.SavePictureChoice(store, profile);
            App.RememberProfile(profile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, Text.Get("The profile could not be saved:") + " " + ex.Message, "Fast Vitae");
        }
        Refresh();
    }

    // Switch, add or delete profiles
    private void Profiles_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = ProfilesButton };
        foreach (string name in store.ListProfiles())
        {
            var item = new MenuItem { Header = name, IsChecked = name == profile };
            item.Click += (s, a) => SwitchTo(name);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());

        var add = new MenuItem { Header = Text.Get("Add profile...") };
        add.Click += (s, a) => AddProfile();
        menu.Items.Add(add);

        var delete = new MenuItem { Header = Text.Format("Delete {0}...", profile) };
        delete.Click += (s, a) => DeleteProfile();
        menu.Items.Add(delete);

        menu.IsOpen = true;
    }

    private void SwitchTo(string name)
    {
        profile = name;
        App.RememberProfile(profile);
        Refresh();
    }

    private void AddProfile()
    {
        var info = new BasicInfo();
        var dialog = new SetupWindow(info, dataFolder: null) { Owner = this, Title = Text.Get("Fast Vitae - New profile") };
        if (dialog.ShowDialog() != true)
            return;

        if (store.ListProfiles().Contains(NameCleaner.Clean(info.FullName), StringComparer.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, Text.Format("There is already a profile called {0}.", info.FullName), "Fast Vitae");
            return;
        }
        string added = store.SaveProfile(info);
        dialog.SavePictureChoice(store, added);
        SwitchTo(added);
    }

    private void DeleteProfile()
    {
        var others = store.ListProfiles().Where(name => name != profile).ToList();
        if (others.Count == 0)
        {
            MessageBox.Show(this, Text.Get("This is the only profile. Add another one before deleting it."), "Fast Vitae");
            return;
        }

        var answer = MessageBox.Show(this,
            Text.Format("Delete the profile {0} with all its CVs and Fields? This cannot be undone.", profile),
            "Fast Vitae", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        store.DeleteProfile(profile);
        SwitchTo(others[0]);
    }
}
