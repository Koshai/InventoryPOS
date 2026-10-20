namespace InventoryPOS.Application.Updates;

public sealed class AppUpdateInfo
{
    public string CurrentVersion { get; init; } = string.Empty;

    public string? LatestVersion { get; init; }

    public string? DownloadUrl { get; init; }

    public string? Sha256 { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsOnlineCheckConfigured { get; init; }

    public bool UpdateAvailable { get; init; }
}
