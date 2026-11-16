using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace FastVitae.Storage;

// One published version
public record Release(Version Version, string DownloadUrl, string PageUrl);

// Finds and installs new versions
public static class Updater
{
    private const string LatestUrl = "https://api.github.com/repos/facundogiovine/FastVitae/releases/latest";
    private const string ExeName = "FastVitae.exe";

    // Release JSON, or null
    public static Release? ParseRelease(string json)
    {
        try
        {
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;
            string tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!Version.TryParse(tag.TrimStart('v'), out var version))
                return null;

            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() == ExeName)
                    return new Release(version, asset.GetProperty("browser_download_url").GetString() ?? "",
                        root.GetProperty("html_url").GetString() ?? "");
            }
            return null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    // Major, minor and build only
    public static bool IsNewer(Version latest, Version current) =>
        new Version(latest.Major, latest.Minor, Math.Max(latest.Build, 0)) >
        new Version(current.Major, current.Minor, Math.Max(current.Build, 0));

    // Newer release, else null
    public static async Task<Release?> CheckAsync()
    {
        try
        {
            using var client = NewClient(TimeSpan.FromSeconds(5));
            var release = ParseRelease(await client.GetStringAsync(LatestUrl));
            var current = Assembly.GetEntryAssembly()?.GetName().Version;
            return release != null && current != null && IsNewer(release.Version, current) ? release : null;
        }
        catch (Exception)
        {
            // Offline or blocked: stay quiet
            return null;
        }
    }

    // Download, delete on failure
    public static async Task DownloadAsync(Release release, string newPath)
    {
        try
        {
            using var client = NewClient(TimeSpan.FromMinutes(5));
            await using var download = await client.GetStreamAsync(release.DownloadUrl);
            await using var file = File.Create(newPath);
            await download.CopyToAsync(file);
        }
        catch
        {
            File.Delete(newPath);
            throw;
        }
    }

    // Running exe can be renamed
    public static void Swap(string exePath, string newPath)
    {
        string oldPath = exePath + ".old";
        File.Delete(oldPath);
        File.Move(exePath, oldPath);
        try
        {
            File.Move(newPath, exePath);
        }
        catch
        {
            File.Move(oldPath, exePath);
            throw;
        }
    }

    // Remove last version's file
    public static void CleanOld(string exePath)
    {
        try
        {
            File.Delete(exePath + ".old");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static HttpClient NewClient(TimeSpan timeout)
    {
        var client = new HttpClient { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FastVitae");
        return client;
    }
}
