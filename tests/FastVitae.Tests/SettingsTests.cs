using System.IO;
using FastVitae.Storage;
using Xunit;

namespace FastVitae.Tests;

public class SettingsTests
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "FastVitaeSettings_" + Guid.NewGuid().ToString("N"));

    private string NewFile => Path.Combine(root, "AppData", "FastVitae", "settings.json");
    private string OldFile => Path.Combine(root, "Exe", "settings.json");

    private void WriteOld(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OldFile)!);
        File.WriteAllText(OldFile, json);
    }

    [Fact]
    public void OldFileNextToExe_IsMoved()
    {
        WriteOld("""{"DataFolder":"Data","LastProfile":"Ana"}""");

        var settings = AppSettings.Load(NewFile, OldFile);

        Assert.Equal("Ana", settings.LastProfile);
        Assert.Equal(Path.Combine(root, "Exe", "Data"), settings.DataFolder);
        Assert.True(File.Exists(NewFile));
        Assert.False(File.Exists(OldFile));
    }

    [Fact]
    public void NewFileWins_OldFileLeftAlone()
    {
        WriteOld("""{"LastProfile":"Old"}""");
        Directory.CreateDirectory(Path.GetDirectoryName(NewFile)!);
        File.WriteAllText(NewFile, """{"LastProfile":"New"}""");

        Assert.Equal("New", AppSettings.Load(NewFile, OldFile).LastProfile);
        Assert.True(File.Exists(OldFile));
    }

    [Fact]
    public void NoFiles_EmptySettings() =>
        Assert.Equal("", AppSettings.Load(NewFile, OldFile).DataFolder);

    [Fact]
    public void Save_CreatesFolder()
    {
        var settings = new AppSettings { LastProfile = "Ana" };
        settings.SetDataFolder(Path.Combine(root, "Data"));
        settings.Save(NewFile);

        var loaded = AppSettings.Load(NewFile, OldFile);
        Assert.Equal(Path.Combine(root, "Data"), loaded.DataFolder);
    }
}
