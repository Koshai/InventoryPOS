namespace InventoryPOS.Application.Licensing;

public sealed class LicenseDocument
{
    public int SchemaVersion { get; set; } = 2;

    public Guid LicenseId { get; set; }

    /// <summary>
    /// Customer identity. For schema 2 this is the customer email.
    /// Schema 1 may use a free-form customer code.
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Licensed customer email (schema 2+). May be empty on schema 1 files.
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    public LicenseEdition Edition { get; set; }

    public string InstallationId { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ValidFromUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? UpdatesThroughUtc { get; set; }

    public int MaxInstallations { get; set; } = 1;

    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Best available email for display (schema 2 CustomerEmail, else CustomerId when it looks like email).
    /// </summary>
    public string ResolveCustomerEmail()
    {
        if (!string.IsNullOrWhiteSpace(CustomerEmail))
            return CustomerEmail.Trim();

        if (!string.IsNullOrWhiteSpace(CustomerId) && CustomerId.Contains('@'))
            return CustomerId.Trim();

        return string.Empty;
    }
}
