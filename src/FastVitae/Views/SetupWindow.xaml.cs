using System.IO;
using System.Windows;
using FastVitae.Model;
using FastVitae.Storage;
using Microsoft.Win32;

namespace FastVitae.Views;

// Basic info and data folder
public partial class SetupWindow : Window
{
    private readonly BasicInfo info;

    // Picked picture, not saved yet
    private System.Windows.Media.Imaging.BitmapSource? chosenPicture;
    private bool pictureRemoved;

    public string DataFolder => FolderBox.Text.Trim();

    // Null dataFolder hides folder row
    public SetupWindow(BasicInfo info, string? dataFolder, string reason = "", string? currentPicture = null)
    {
        InitializeComponent();
        this.info = info;
        DataContext = info;

        // Language only on first run
        if (dataFolder == null)
        {
            FolderPanel.Visibility = Visibility.Collapsed;
            LanguageBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            FolderBox.Text = dataFolder;
            LanguageBox.SelectedValue = Text.Language;
        }

        if (reason != "")
        {
            ReasonText.Text = reason;
            ReasonText.Visibility = Visibility.Visible;
        }

        if (currentPicture != null && File.Exists(currentPicture))
            ShowPicture(currentPicture);

        Loaded += (s, e) => FirstNameBox.Focus();
    }

    // Apply the picture choice
    public void SavePictureChoice(ProfileStore store, string profile)
    {
        if (chosenPicture != null)
            store.SavePicture(profile, chosenPicture);
        else if (pictureRemoved)
            store.DeletePicture(profile);
    }

    private void ShowPicture(string path) => ShowPicture(PictureFile.Load(path));

    private void ShowPicture(System.Windows.Media.Imaging.BitmapSource picture)
    {
        PictureImage.Source = picture;
        RemovePictureButton.Visibility = Visibility.Visible;
    }

    private void ChoosePicture_Click(object sender, RoutedEventArgs e)
    {
        var picture = App.PickPicture(this, info.FullName, info.Title);
        if (picture == null)
            return;
        ShowPicture(picture);
        chosenPicture = picture;
        pictureRemoved = false;
    }

    private void RemovePicture_Click(object sender, RoutedEventArgs e)
    {
        PictureImage.Source = null;
        RemovePictureButton.Visibility = Visibility.Collapsed;
        chosenPicture = null;
        pictureRemoved = true;
    }

    private void Language_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LanguageBox.SelectedValue is string code)
            Text.SetLanguage(code);
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Text.Get("Folder where Fast Vitae keeps your data") };
        if (dialog.ShowDialog(this) == true)
            FolderBox.Text = dialog.FolderName;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (info.FirstName.Trim() == "" || info.LastName.Trim() == "")
        {
            MessageBox.Show(this, Text.Get("Please enter your first and last name."), "Fast Vitae");
            return;
        }
        if (FolderPanel.Visibility == Visibility.Visible && DataFolder == "")
        {
            MessageBox.Show(this, Text.Get("Please choose a folder for your data."), "Fast Vitae");
            return;
        }
        DialogResult = true;
    }
}
