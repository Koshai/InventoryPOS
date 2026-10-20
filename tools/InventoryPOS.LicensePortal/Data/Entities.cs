namespace InventoryPOS.LicensePortal.Data;

public sealed class Customer
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public List<MachineLicense> Machines { get; set; } = new();
}

public sealed class MachineLicense
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string InstallationId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public Guid LicenseId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string FeaturesJson { get; set; } = "[]";
    public string LastIssuedFileName { get; set; } = string.Empty;

    public List<IssueHistory> History { get; set; } = new();
}

public sealed class IssueHistory
{
    public long Id { get; set; }
    public long MachineLicenseId { get; set; }
    public MachineLicense MachineLicense { get; set; } = null!;
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string IssuedByUserName { get; set; } = string.Empty;
}

public sealed class AppRelease
{
    public long Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
    public string PublishedByUserName { get; set; } = string.Empty;
}
