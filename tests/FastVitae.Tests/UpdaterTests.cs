using System.IO;
using FastVitae.Storage;
using Xunit;

namespace FastVitae.Tests;

public class UpdaterTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("fv-update-").FullName;

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static string ReleaseJson(string tag, string assetName) => $$"""
        {
          "tag_name": "{{tag}}",
          "html_url": "https://github.com/facundogiovine/FastVitae/releases/tag/{{tag}}",
          "assets": [
            { "name": "{{assetName}}", "browser_download_url": "https://example.test/{{assetName}}" }
          ]
        }
        """;

    [Fact]
    public void ParseRelease_Valid()
    {
        var release = Updater.ParseRelease(ReleaseJson("v1.3.0", "FastVitae.exe"))!;
        Assert.Equal(new Version(1, 3, 0), release.Version);
        Assert.Equal("https://example.test/FastVitae.exe", release.DownloadUrl);
        Assert.Equal("https://github.com/facundogiovine/FastVitae/releases/tag/v1.3.0", release.PageUrl);
    }

    [Fact]
    public void ParseRelease_NoExeAsset_Null() => Assert.Null(Updater.ParseRelease(ReleaseJson("v1.3.0", "notes.txt")));

    [Fact]
    public void ParseRelease_BetaTag_Null() => Assert.Null(Updater.ParseRelease(ReleaseJson("v1.3.0-beta", "FastVitae.exe")));

    [Fact]
    public void ParseRelease_Garbage_Null() => Assert.Null(Updater.ParseRelease("<html>rate limited</html>"));

    [Theory]
    [InlineData("1.2.4", "1.2.3.0", true)]
    [InlineData("1.2.3", "1.2.3.0", false)]
    [InlineData("1.2.2", "1.2.3.0", false)]
    [InlineData("2.0.0", "1.9.9.0", true)]
    public void IsNewer(string latest, string current, bool expected) =>
        Assert.Equal(expected, Updater.IsNewer(Version.Parse(latest), Version.Parse(current)));

    [Fact]
    public void Swap_ReplacesFile()
    {
        string exe = Path.Combine(folder, "FastVitae.exe"), next = exe + ".new";
        File.WriteAllText(exe, "old");
        File.WriteAllText(next, "new");
        Updater.Swap(exe, next);
        Assert.Equal("new", File.ReadAllText(exe));
        Assert.Equal("old", File.ReadAllText(exe + ".old"));
        Assert.False(File.Exists(next));
    }

    [Fact]
    public void Swap_LeftoverOld_Replaced()
    {
        string exe = Path.Combine(folder, "FastVitae.exe"), next = exe + ".new";
        File.WriteAllText(exe, "old");
        File.WriteAllText(exe + ".old", "older");
        File.WriteAllText(next, "new");
        Updater.Swap(exe, next);
        Assert.Equal("new", File.ReadAllText(exe));
        Assert.Equal("old", File.ReadAllText(exe + ".old"));
    }

    [Fact]
    public void CleanOld_Locked_DoesNotThrow()
    {
        string exe = Path.Combine(folder, "FastVitae.exe");
        File.WriteAllText(exe + ".old", "old");
        using (new FileStream(exe + ".old", FileMode.Open, FileAccess.Read, FileShare.None))
            Updater.CleanOld(exe);
        Updater.CleanOld(exe);
        Assert.False(File.Exists(exe + ".old"));
    }

    [Fact]
    public void Undo_PutsOldExeBack()
    {
        string exe = Path.Combine(folder, "FastVitae.exe"), next = exe + ".new";
        File.WriteAllText(exe, "old");
        File.WriteAllText(next, "new");
        Updater.Swap(exe, next);
        Updater.Undo(exe);
        Assert.Equal("old", File.ReadAllText(exe));
        Assert.False(File.Exists(exe + ".old"));
    }

    [Fact]
    public void IsNetworkError_DroppedDownload() =>
        Assert.True(Updater.IsNetworkError(new System.Net.Http.HttpIOException(System.Net.Http.HttpRequestError.ResponseEnded)));

    [Fact]
    public void IsNetworkError_RequestAndTimeout()
    {
        Assert.True(Updater.IsNetworkError(new System.Net.Http.HttpRequestException()));
        Assert.True(Updater.IsNetworkError(new TaskCanceledException()));
    }

    [Fact]
    public void IsNetworkError_DiskErrors_False()
    {
        Assert.False(Updater.IsNetworkError(new IOException()));
        Assert.False(Updater.IsNetworkError(new UnauthorizedAccessException()));
    }
}
