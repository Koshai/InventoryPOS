namespace InventoryPOS.Application.Licensing;

public sealed class SignedLicense
{
    public string KeyId { get; set; } = string.Empty;

    public string PayloadBase64 { get; set; } = string.Empty;

    public string SignatureBase64 { get; set; } = string.Empty;
}

public sealed class LicenseValidationResult
{
    public LicenseStatus Status { get; init; }

    public LicenseDocument? License { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsUsable =>
        Status is LicenseStatus.Valid
            or LicenseStatus.ExpiringSoon;
}