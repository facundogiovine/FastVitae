using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using FastVitae.Model;
using FastVitae.Rendering;
using FastVitae.Storage;
using Microsoft.Web.WebView2.Core;

namespace FastVitae.Views;

// Form with live preview
public partial class EditorWindow : Window
{
    private readonly ProfileStore store;
    private readonly string profile;
    private readonly CvDocument doc;

    // Set when editing a CV.
    private string company = "";
    private string role = "";

    // Set when editing a Specialty.
    private string field = "";
    private string specialty = "";

    private bool IsSpecialty => specialty != "";

    // Content last written to disk
    private string savedJson = "";

    private readonly DispatcherTimer previewTimer;
    private bool previewReady;

    public static EditorWindow ForCv(ProfileStore store, string profile, CvDocument doc, string company, string role, bool alreadyCreated)
    {
        var window = new EditorWindow(store, profile, doc) { company = company, role = role };
        window.Title = $"Fast Vitae - {company} - {role}";
        if (alreadyCreated)
        {
            window.savedJson = window.CurrentJson();
            window.OpenFolderButton.Visibility = Visibility.Visible;
        }
        return window;
    }

    public static EditorWindow ForSpecialty(ProfileStore store, string profile, CvDocument doc, string field, string specialty)
    {
        var window = new EditorWindow(store, profile, doc) { field = field, specialty = specialty };
        window.Title = $"Fast Vitae - {field} - {specialty}";
        window.savedJson = window.CurrentJson();

        // Hide the CV-only parts
        window.BasicInfoExpander.Visibility = Visibility.Collapsed;
        window.PicturePanel.Visibility = Visibility.Collapsed;
        window.SaveToSpecialtyButton.Visibility = Visibility.Collapsed;
        window.CreateButton.Content = Text.Get("Save");
        window.CreateButton.ToolTip = Text.Get("Save this content");
        return window;
    }

    private EditorWindow(ProfileStore store, string profile, CvDocument doc)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;
        this.doc = doc;
        DataContext = doc;
        ShowPictureNote();

        // Redraw shortly after typing
        previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        previewTimer.Tick += async (s, e) =>
        {
            previewTimer.Stop();
            await ShowCvInPreview();
        };

        // Text changes refresh the preview
        FormPanel.AddHandler(TextBoxBase.TextChangedEvent, new RoutedEventHandler((s, e) => ContentChanged()));
        FormPanel.AddHandler(Selector.SelectionChangedEvent, new RoutedEventHandler((s, e) => ContentChanged()));
        Loaded += async (s, e) => await StartPreview();
    }

    private string CurrentJson() => JsonSerializer.Serialize(doc, CvDocument.JsonOptions);

    private void ContentChanged()
    {
        StatusText.Text = "";
        previewTimer.Stop();
        previewTimer.Start();
    }

    // ---- Preview ----

    private async Task StartPreview()
    {
        try
        {
            // Cache in local app data
            string cacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastVitae", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, cacheFolder);
            await Preview.EnsureCoreWebView2Async(environment);

            Preview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Preview.CoreWebView2.Settings.IsStatusBarEnabled = false;
            // Block navigation from links
            Preview.CoreWebView2.NavigationStarting += (s, e) => e.Cancel = e.IsUserInitiated;
            Preview.CoreWebView2.NewWindowRequested += (s, e) => e.Handled = true;

            Preview.NavigationCompleted += (s, e) =>
            {
                previewReady = true;
                FitPreviewToWidth();
            };
            Preview.NavigateToString(HtmlRenderer.RenderPage(doc, store.PicturePath(profile)));
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or IOException or UnauthorizedAccessException)
        {
            Preview.Visibility = Visibility.Collapsed;
            PreviewError.Text = Text.Get("The preview and the PDF need the Microsoft Edge WebView2 Runtime, which was not found on this PC.");
            PreviewError.Visibility = Visibility.Visible;
        }
    }

    // Replace CV, keep scroll
    private async Task ShowCvInPreview()
    {
        if (!previewReady)
            return;
        string body = JsonSerializer.Serialize(HtmlRenderer.RenderBody(doc, store.PicturePath(profile)));
        await Preview.ExecuteScriptAsync($"document.getElementById('page').innerHTML = {body};");
    }

    private void Preview_SizeChanged(object sender, SizeChangedEventArgs e) => FitPreviewToWidth();

    // Zoom to fit sheet width
    private void FitPreviewToWidth()
    {
        const double sheetWidth = 880;  // sheet plus border, pixels
        if (previewReady && Preview.ActualWidth > 0)
            Preview.ZoomFactor = Math.Clamp(Preview.ActualWidth / sheetWidth, 0.3, 1.0);
    }

    // ---- Form buttons ----

    private void ShowPictureNote() =>
        NoPictureNote.Visibility = store.HasPicture(profile) ? Visibility.Collapsed : Visibility.Visible;

    // No picture: ask one
    private void PictureOption_Click(object sender, RoutedEventArgs e)
    {
        if (doc.ShowPicture && !store.HasPicture(profile) && !App.ChoosePicture(this, store, profile))
            PictureOption.IsChecked = false;
        ShowPictureNote();
        ContentChanged();
    }

    private void ChangePicture_Click(object sender, RoutedEventArgs e)
    {
        if (App.ChoosePicture(this, store, profile))
            ContentChanged();
        ShowPictureNote();
    }

    private void Country_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => ContentChanged();

    private void AddSection_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddSectionWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            doc.Sections.Add(dialog.Section!);
            ContentChanged();
        }
    }

    private void RemoveSection_Click(object sender, RoutedEventArgs e)
    {
        doc.Sections.Remove(ItemOf<Section>(sender));
        ContentChanged();
    }

    private void MoveSectionUp_Click(object sender, RoutedEventArgs e) => MoveSection(ItemOf<Section>(sender), -1);

    private void MoveSectionDown_Click(object sender, RoutedEventArgs e) => MoveSection(ItemOf<Section>(sender), +1);

    private void MoveSection(Section section, int step)
    {
        int from = doc.Sections.IndexOf(section);
        int to = from + step;
        if (to < 0 || to >= doc.Sections.Count)
            return;
        doc.Sections.Move(from, to);
        ContentChanged();
    }

    private void AddEntry_Click(object sender, RoutedEventArgs e)
    {
        // New entry gets one role
        var entry = new Entry();
        entry.Roles.Add(new Role());
        ItemOf<Section>(sender).Entries.Add(entry);
        ContentChanged();
    }

    private void RemoveEntry_Click(object sender, RoutedEventArgs e)
    {
        var entry = ItemOf<Entry>(sender);
        doc.Sections.First(section => section.Entries.Contains(entry)).Entries.Remove(entry);
        ContentChanged();
    }

    private void AddRole_Click(object sender, RoutedEventArgs e)
    {
        ItemOf<Entry>(sender).Roles.Add(new Role());
        ContentChanged();
    }

    private void RemoveRole_Click(object sender, RoutedEventArgs e)
    {
        var role = ItemOf<Role>(sender);
        var entry = doc.Sections.SelectMany(section => section.Entries).First(en => en.Roles.Contains(role));
        entry.Roles.Remove(role);
        ContentChanged();
    }

    private void AddColumn_Click(object sender, RoutedEventArgs e)
    {
        var section = ItemOf<Section>(sender);
        if (section.Columns.Count >= Section.MaxColumns)
            return;
        section.Columns.Add(new BulletColumn());
        ContentChanged();
    }

    private void RemoveColumn_Click(object sender, RoutedEventArgs e)
    {
        var column = ItemOf<BulletColumn>(sender);
        doc.Sections.First(section => section.Columns.Contains(column)).Columns.Remove(column);
        ContentChanged();
    }

    private void AddColumnEntry_Click(object sender, RoutedEventArgs e)
    {
        ItemOf<BulletColumn>(sender).Entries.Add(new ColumnEntry());
        ContentChanged();
    }

    private void RemoveColumnEntry_Click(object sender, RoutedEventArgs e)
    {
        var entry = ItemOf<ColumnEntry>(sender);
        doc.Sections.SelectMany(section => section.Columns).First(column => column.Entries.Contains(entry)).Entries.Remove(entry);
        ContentChanged();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e) => App.OpenFolder(store.FolderToShow(profile, company));

    // Format window for one field
    public void EditStyle(System.Windows.Controls.Control box, string key)
    {
        var styles = box.DataContext switch
        {
            Role role => role.Styles,
            Entry entry => entry.Styles,
            Section section => section.Styles,
            ColumnEntry bullet => bullet.Styles,
            _ => doc.Styles
        };
        var start = styles.TryGetValue(key, out var saved) ? saved.Copy() : TextStyle.DefaultFor(key);
        string sample = box switch
        {
            System.Windows.Controls.TextBox textBox => textBox.Text,
            System.Windows.Controls.ComboBox => ((Role)box.DataContext).Dates,
            _ => ""
        };

        // Open just below the box
        var dialog = new StyleWindow(key, start, sample) { Owner = this };
        var corner = box.PointToScreen(new Point(0, box.ActualHeight + 4));
        var source = PresentationSource.FromVisual(box);
        if (source?.CompositionTarget != null)
            corner = source.CompositionTarget.TransformFromDevice.Transform(corner);
        dialog.Left = corner.X;
        dialog.Top = corner.Y;

        if (dialog.ShowDialog() != true)
            return;
        // One field, or all
        var targets = !dialog.ApplyToAll ? new List<Dictionary<string, TextStyle>> { styles }
            : key is "EntryName" or "Place"
                ? doc.Sections.SelectMany(s => s.Entries).Select(en => en.Styles).ToList()
                : doc.Sections.SelectMany(s => s.Entries).SelectMany(en => en.Roles).Select(r => r.Styles).ToList();
        foreach (var target in targets)
        {
            if (dialog.Result == null)
                target.Remove(key);
            else
                target[key] = dialog.Result.Copy();
        }
        ContentChanged();
    }

    // Item behind the clicked button
    private static T ItemOf<T>(object button) => (T)((FrameworkElement)button).DataContext;

    // ---- Create / Save ----

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (IsSpecialty)
            {
                store.SaveSpecialty(profile, field, specialty, doc);
                StatusText.Text = Text.Get("Saved");
            }
            else
            {
                string pdfPath = await CreateCvFiles();
                StatusText.Text = Text.Get("Created in") + " " + store.OutputFolder(profile, company);
                OpenFolderButton.Visibility = Visibility.Visible;
                RecordApplication(pdfPath);
            }
            savedJson = CurrentJson();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                Text.Get("The files could not be written. If the PDF or Word file is open in another program, close it and try again.") + "\n\n" + ex.Message,
                "Fast Vitae", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // Add to the applications Excel
    private void RecordApplication(string pdfPath)
    {
        try
        {
            string field = doc.Field == "" ? "General" : doc.Field;
            string cvPath = Path.GetRelativePath(store.ProfileFolder(profile), pdfPath);
            new ApplicationStore(store).Record(profile, field, NameCleaner.Clean(company), NameCleaner.Clean(role), cvPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                Text.Get("The CV was created, but the applications list could not be updated. If its Excel file is open, close it and click Create again.") + "\n\n" + ex.Message,
                "Fast Vitae", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // Write data, Word and PDF
    private async Task<string> CreateCvFiles()
    {
        store.SaveCv(profile, company, role, doc);

        string folder = store.OutputFolder(profile, company);
        Directory.CreateDirectory(folder);
        string fileName = NameCleaner.Clean($"{doc.BasicInfo.FullName} - {role}");
        if (HtmlRenderer.UsesPicture(doc, store.PicturePath(profile)))
            fileName += "_Pic";
        DocxWriter.Write(doc, Path.Combine(folder, fileName + ".docx"), store.PicturePath(profile));

        if (!previewReady)
            throw new IOException(Text.Get("The PDF was not created because the preview is not available. The Word file was created."));

        // Print the up-to-date preview
        previewTimer.Stop();
        await ShowCvInPreview();

        var settings = Preview.CoreWebView2.Environment.CreatePrintSettings();
        settings.PageWidth = 8.5;
        settings.PageHeight = 11;
        settings.MarginTop = 0.5;
        settings.MarginBottom = 0.5;
        settings.MarginLeft = 0.4;
        settings.MarginRight = 0.4;
        settings.ShouldPrintBackgrounds = false;
        settings.ShouldPrintHeaderAndFooter = false;

        string pdfPath = Path.Combine(folder, fileName + ".pdf");
        bool printed = await Preview.CoreWebView2.PrintToPdfAsync(pdfPath, settings);
        if (!printed)
            throw new IOException(Text.Get("The PDF could not be written:") + " " + pdfPath);
        return pdfPath;
    }

    private void SaveToSpecialty_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveToSpecialtyWindow(store, profile) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            store.SaveSpecialty(profile, dialog.Field, dialog.Specialty, doc.Copy());
            StatusText.Text = Text.Format("Saved to {0} - {1}", dialog.Field, dialog.Specialty);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, Text.Get("The Specialty could not be saved:") + " " + ex.Message, "Fast Vitae");
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (CurrentJson() == savedJson)
            return;

        string question = IsSpecialty
            ? Text.Get("Your changes were not saved. Close anyway?")
            : Text.Get("This CV has changes that were not created. Close anyway?");
        var answer = MessageBox.Show(this, question, "Fast Vitae",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        e.Cancel = answer != MessageBoxResult.Yes;
    }

    // Release the browser on close
    protected override void OnClosed(EventArgs e)
    {
        previewTimer.Stop();
        Preview.Dispose();
        base.OnClosed(e);
    }
}
