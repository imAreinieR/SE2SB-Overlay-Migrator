using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

public static class UpdaterService
{
    private static readonly TimeSpan DefaultTimeOut = TimeSpan.FromSeconds(60);
    private const           string   Repo           = "imAreinieR/SE2SB-Overlay-Migrator";
    private const           string   ReleasesPage   = $"https://github.com/{Repo}/releases";

    public static async Task<string?> CheckForLatestAsync()
    {
        try
        {
            string currentVersion = GetCurrentVersion();

            using var client = new HttpClient { Timeout = DefaultTimeOut };
            client
                .DefaultRequestHeaders
                .UserAgent
                .Add(new ProductInfoHeaderValue("SE2SB Overlay Migrator", currentVersion));

            string json = await client.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest");

            JsonElement release   = JsonDocument.Parse(json).RootElement;
            string?     latestTag = release.GetProperty("tag_name").GetString();

            if (latestTag is null)
                return null;

            if (!TryParseGitTag(latestTag, out Version? latestVersion) || !TryParseGitTag(currentVersion, out Version? currentParsed))
                return null;

            return latestVersion > currentParsed
                ? $"Version {latestTag} is available."
                : "Up-to-date!";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[{nameof(UpdaterService)}] Version check failed: {exception}");
            return null;
        }
    }

    public static async Task CheckAndUpdateToLatestAsync()
    {
        try
        {
            string currentVersion = GetCurrentVersion();

            using var client = new HttpClient { Timeout = DefaultTimeOut };
            client
                .DefaultRequestHeaders
                .UserAgent
                .Add(new ProductInfoHeaderValue("SE2SB Overlay Migrator", currentVersion));

            string? json = await client.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest");

            JsonElement release   = JsonDocument.Parse(json).RootElement;
            string?     latestTag = release.GetProperty("tag_name").GetString();

            if
            (
                latestTag != null
                    && TryParseGitTag(latestTag, out Version? latestVersion)
                    && TryParseGitTag(currentVersion, out Version? currentParsed)
                    && latestVersion <= currentParsed
            )
                return;

            // Find the .zip and .sha256 assets
            string? downloadUrl = null;
            string? checksumUrl = null;

            foreach (JsonElement asset in release.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? string.Empty;

                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                else if (name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                    checksumUrl = asset.GetProperty("browser_download_url").GetString();
            }

            if (downloadUrl is null)
            {
                PromptManual(latestTag!);
                return;
            }

            var result = MessageBox.Show
            (
                $"Version {latestTag} is available.\n\nUpdate now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information
            );

            if (result == MessageBoxResult.Yes)
                LaunchUpdaterAndExit(downloadUrl, checksumUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{nameof(UpdaterService)}] Update check failed: {ex}");
        }
    }

    private static string GetCurrentVersion()
    {
        string currentVersion = "v0.1.0";
        Version? version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        if (version != null)
            currentVersion = $"v{version.Major}.{version.Minor}.{version.Build}";

        return currentVersion;
    }

    private static bool TryParseGitTag(string tag, out Version? version)
    {
        string normalized = tag.TrimStart('v');

        int dashIndex = normalized.IndexOf('-');
        if (dashIndex >= 0)
            normalized = normalized[..dashIndex];

        return Version.TryParse(normalized, out version);
    }

    private static void LaunchUpdaterAndExit(string downloadUrl, string? checksumUrl)
    {
        string updaterPath = Path.Combine(AppContext.BaseDirectory, "ApplicationUpdater.exe");

        if (!File.Exists(updaterPath))
        {
            PromptManual(null);
            return;
        }

        string? targetPath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;

        if (targetPath is null)
        {
            PromptManual(null);
            return;
        }

        string arguments = $"--url \"{downloadUrl}\" --target \"{targetPath}\"";

        if (checksumUrl is not null)
            arguments += $" --checksum \"{checksumUrl}\"";

        Process.Start
        (
            new ProcessStartInfo
            {
                FileName        = updaterPath,
                Arguments       = arguments,
                UseShellExecute = true
            }
        );

        Application.Current.Shutdown();
    }

    private static void PromptManual(string? latestTag)
    {
        string message = latestTag is not null
            ? $"Version {latestTag} is available but the updater couldn't run automatically.\n\nOpen the Releases page?"
            : "A new version is available but the updater is missing.\n\nOpen the releases page?";

        MessageBoxResult result = MessageBox.Show
        (
            message,
            "Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information
        );

        if (result == MessageBoxResult.Yes)
            Process.Start
            (
                new ProcessStartInfo(ReleasesPage)
                {
                    UseShellExecute = true
                }
            );
    }
}