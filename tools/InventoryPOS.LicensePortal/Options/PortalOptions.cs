namespace InventoryPOS.LicensePortal.Options;

public sealed class PortalOptions
{
    public const string SectionName = "Portal";

    /// <summary>Directory containing private-key.pem and public-key.pem.</summary>
    public string KeyDirectory { get; set; } = "/data/keys";

    /// <summary>Directory where issued .iposlic files are stored.</summary>
    public string IssuedLicensesDirectory { get; set; } = "/data/issued-licenses";

    /// <summary>Directory where uploaded installer binaries are stored.</summary>
    public string ReleasesDirectory { get; set; } = "/data/releases";

    /// <summary>Public base URL of this portal (used in update feed downloadUrl).</summary>
    public string PublicBaseUrl { get; set; } = "https://licenses.example.com";

    /// <summary>Bootstrap admin email (created once if no users exist).</summary>
    public string BootstrapAdminEmail { get; set; } = "admin@example.com";

    /// <summary>Bootstrap admin password (change after first login).</summary>
    public string BootstrapAdminPassword { get; set; } = string.Empty;
}
