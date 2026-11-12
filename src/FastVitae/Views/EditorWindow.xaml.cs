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
        window.Title = $"FastVitae - {company} - {role}";
        if (alreadyCreated)
            window.savedJson = window.CurrentJson();
        return window;
    }

    public static EditorWindow ForSpecialty(ProfileStore store, string profile, CvDocument doc, string field, string specialty)
    {
        var window = new EditorWindow(store, profile, doc) { field = field, specialty = specialty };
        window.Title = $"FastVitae - {field} - {specialty}";
        window.savedJson = window.CurrentJson();

        // Hide the CV-only parts
        window.BasicInfoExpander.Visibility = Visibility.Collapsed;
        window.PictureOption.Visibility = Visibility.Collapsed;
        window.SaveToSpecialtyButton.Visibility = Visibility.Collapsed;
        window.CreateButton.Content = "Save";
        window.CreateButton.ToolTip = "Save this content";
        return window;
    }

    private EditorWindow(ProfileStore store, string profile, CvDocument doc)
    {
        InitializeComponent();
        this.store = store;
        this.profile = profile;
        this.doc = doc;
        DataContext = doc;

        if (!store.HasPicture(profile))
        {
            PictureOption.IsEnabled = false;
            PictureOption.ToolTip = "Add a picture in Profile first";
        }

        // Redraw shortly after typing
        previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        previewTimer.Tick += async (s, e) =>
        {
            previewTimer.Stop();
            await ShowCvInPreview();
        };

        // Text changes refresh the preview
        FormPanel.AddHandler(TextBoxBase.TextChangedEvent, new RoutedEventHandler((s, e) => ContentChanged()));
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
            PreviewError.Text = "The preview and the PDF need the Microsoft Edge WebView2 Runtime, which was not found on this PC.";
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

    private void PictureOption_Click(object sender, RoutedEventArgs e) => ContentChanged();

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
                StatusText.Text = "Saved";
            }
            else
            {
                await CreateCvFiles();
                StatusText.Text = "Created in " + store.CvFolder(profile, company);
            }
            savedJson = CurrentJson();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                "The files could not be written. If the PDF or Word file is open in another program, close it and try again.\n\n" + ex.Message,
                "FastVitae", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // Write data, Word and PDF
    private async Task CreateCvFiles()
    {
        store.SaveCv(profile, company, role, doc);

        string folder = store.CvFolder(profile, company);
        string fileName = NameCleaner.Clean($"{doc.BasicInfo.FullName} - {role}");
        DocxWriter.Write(doc, Path.Combine(folder, fileName + ".docx"), store.PicturePath(profile));

        if (!previewReady)
            throw new IOException("The PDF was not created because the preview is not available. The Word file was created.");

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
            throw new IOException("The PDF could not be written: " + pdfPath);
    }

    private void SaveToSpecialty_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveToSpecialtyWindow(store, profile) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            store.SaveSpecialty(profile, dialog.Field, dialog.Specialty, doc.Copy());
            StatusText.Text = $"Saved to {dialog.Field} - {dialog.Specialty}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "The Specialty could not be saved: " + ex.Message, "FastVitae");
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (CurrentJson() == savedJson)
            return;

        string question = IsSpecialty
            ? "Your changes were not saved. Close anyway?"
            : "This CV has changes that were not created. Close anyway?";
        var answer = MessageBox.Show(this, question, "FastVitae",
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
