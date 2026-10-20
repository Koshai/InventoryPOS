using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace InventoryPOS.Desktop.Services;

public sealed class AppUpdateService : IAppUpdateService
{
    private readonly IConfiguration _configuration;

    public AppUpdateService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetCurrentVersion()
    {
        try
        {
            var packageVersion = Package.Current.Id.Version;
            return $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
        }
        catch (InvalidOperationException)
        {
            // Unpackaged WinUI install (Inno / Program Files).
        }

        var informational = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }

        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
    }

    public async Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();
        var feedUrl = _configuration["Updates:FeedUrl"];

        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return new AppUpdateInfo
            {
                CurrentVersion = current,
                IsOnlineCheckConfigured = false,
                UpdateAvailable = false,
                Message =
                    "Online updates are not configured. " +
                    "Set Updates:FeedUrl in ProgramData appsettings.Local.json " +
                    "(see docs/appsettings.Local.updates.example.json), " +
                    "or use “Upgrade from installer file…”."
            };
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var response = await client.GetAsync(feedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var feed = await JsonSerializer.DeserializeAsync<UpdateFeedDocument>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (feed is null || string.IsNullOrWhiteSpace(feed.Version))
            {
                return new AppUpdateInfo
                {
                    CurrentVersion = current,
                    IsOnlineCheckConfigured = true,
                    Message = "The update feed returned an invalid response."
                };
            }

            var available = IsNewerVersion(feed.Version, current);

            return new AppUpdateInfo
            {
                CurrentVersion = current,
                LatestVersion = feed.Version,
                DownloadUrl = feed.DownloadUrl,
                Sha256 = feed.Sha256,
                IsOnlineCheckConfigured = true,
                UpdateAvailable = available,
                Message = available
                    ? $"Version {feed.Version} is available (you have {current})."
                    : $"You are on the latest version ({current})."
            };
        }
        catch (Exception ex)
        {
            return new AppUpdateInfo
            {
                CurrentVersion = current,
                IsOnlineCheckConfigured = true,
                Message = "Could not check for updates: " + ex.Message
            };
        }
    }

    public Task LaunchInstallerAndExitAsync(string setupExePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(setupExePath) || !File.Exists(setupExePath))
            throw new FileNotFoundException("Installer file was not found.", setupExePath);

        if (!string.Equals(Path.GetExtension(setupExePath), ".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Please select an InventoryPOSSetup.exe installer.");

        var startInfo = new ProcessStartInfo
        {
            FileName = setupExePath,
            UseShellExecute = true,
            Verb = "runas"
        };

        Process.Start(startInfo);

        // Allow the installer to replace Program Files while this process exits.
        // Database, ProgramData config, license, and DataRoot are not touched by a normal upgrade.
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            App.Current?.Exit();
        });

        return Task.CompletedTask;
    }

    public async Task DownloadAndLaunchUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
            throw new InvalidOperationException("No download URL was provided by the update feed.");

        var tempDir = Path.Combine(Path.GetTempPath(), "InventoryPOS", "Updates");
        Directory.CreateDirectory(tempDir);
        var targetPath = Path.Combine(tempDir, "InventoryPOSSetup.exe");

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        using var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (var remote = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var local = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            await remote.CopyToAsync(local, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(update.Sha256))
        {
            var actual = await ComputeSha256HexAsync(targetPath, cancellationToken);
            if (!string.Equals(actual, update.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(targetPath);
                throw new InvalidOperationException(
                    "Downloaded installer failed checksum verification. The file was deleted.");
            }
        }

        await LaunchInstallerAndExitAsync(targetPath, cancellationToken);
    }

    private static async Task<string> ComputeSha256HexAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (!Version.TryParse(Normalize(latest), out var latestVersion))
            return false;
        if (!Version.TryParse(Normalize(current), out var currentVersion))
            return true;
        return latestVersion > currentVersion;
    }

    private static string Normalize(string version)
    {
        var v = version.Trim();
        var plus = v.IndexOf('+');
        if (plus > 0) v = v[..plus];
        var dash = v.IndexOf('-');
        if (dash > 0) v = v[..dash];
        return v;
    }

    private sealed class UpdateFeedDocument
    {
        public string? Version { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Sha256 { get; set; }
        public string? Notes { get; set; }
    }
}
