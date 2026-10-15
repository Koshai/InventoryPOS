namespace InventoryPOS.Application.Licensing;

public interface ILicenseValidator
{
    LicenseValidationResult Validate(
        SignedLicense signedLicense,
        string installationId,
        DateTimeOffset currentUtc);
}