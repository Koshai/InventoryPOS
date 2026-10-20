using InventoryPOS.Application.Updates;

namespace InventoryPOS.Application.Interfaces;

public interface IAppUpdateService
{
    string GetCurrentVersion();

    /// <summary>
    /// Checks an optional HTTPS feed for a newer installer.
    /// When the feed URL is not configured, returns a clear "not configured" result.
    /// </summary>
    Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Launches a local InventoryPOSSetup.exe with elevation, then signals the app should exit.
    /// Does not modify the database or ProgramData preferences.
    /// </summary>
    Task LaunchInstallerAndExitAsync(string setupExePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the installer from <see cref="AppUpdateInfo.DownloadUrl"/> (when available),
    /// verifies SHA-256 when provided, then launches it.
    /// </summary>
    Task DownloadAndLaunchUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default);
}
