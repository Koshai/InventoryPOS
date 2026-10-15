
namespace InventoryPOS.Application.Licensing;

public sealed class LicenseDocument
{
    public int SchemaVersion { get; set; } = 1;

    public Guid LicenseId { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public LicenseEdition Edition { get; set; }

    public string InstallationId { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ValidFromUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? UpdatesThroughUtc { get; set; }

    public int MaxInstallations { get; set; } = 1;

    public List<string> Features { get; set; } = new();
}
