using System.Security.Cryptography;
using System.Text.Json;
using InventoryPOS.Application.Licensing;

namespace InventoryPOS.Licensing.Issuer;

public sealed class LicenseIssuerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly LicenseKeyStore _keys;
    private readonly string _keyId;

    public LicenseIssuerService(LicenseKeyStore keys, string? keyId = null)
    {
        _keys = keys;
        _keyId = string.IsNullOrWhiteSpace(keyId)
            ? LicenseKeyStore.DefaultKeyId
            : keyId;
    }

    public LicenseDocument CreateBetaLicense(
        string customerEmail,
        string customerName,
        string installationId,
        int days)
    {
        if (string.IsNullOrWhiteSpace(customerEmail) || !customerEmail.Contains('@'))
            throw new ArgumentException("A valid customer email is required.", nameof(customerEmail));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));

        if (string.IsNullOrWhiteSpace(installationId))
            throw new ArgumentException("Installation ID is required.", nameof(installationId));

        if (days < 1 || days > 3650)
            throw new ArgumentOutOfRangeException(nameof(days), "Duration must be between 1 and 3650 days.");

        var email = customerEmail.Trim();
        var now = DateTimeOffset.UtcNow;

        return new LicenseDocument
        {
            SchemaVersion = 2,
            LicenseId = Guid.NewGuid(),
            CustomerId = email,
            CustomerEmail = email,
            CustomerName = customerName.Trim(),
            Edition = LicenseEdition.Beta,
            InstallationId = installationId.Trim(),
            IssuedAtUtc = now,
            ValidFromUtc = now,
            ExpiresAtUtc = now.AddDays(days),
            MaxInstallations = 1,
            Features = new List<string> { "POS", "INVENTORY", "PURCHASING" }
        };
    }

    public SignedLicense Sign(LicenseDocument license)
    {
        ArgumentNullException.ThrowIfNull(license);

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(license);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_keys.ReadPrivateKeyPem());

        byte[] signature = rsa.SignData(
            payload,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        return new SignedLicense
        {
            KeyId = _keyId,
            PayloadBase64 = Convert.ToBase64String(payload),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
    }

    public string WriteLicenseFile(SignedLicense signedLicense, Guid licenseId, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"{licenseId:N}.iposlic");
        File.WriteAllText(outputPath, JsonSerializer.Serialize(signedLicense, JsonOptions));
        return outputPath;
    }

    public string IssueBetaToFile(
        string customerEmail,
        string customerName,
        string installationId,
        int days,
        string outputDirectory)
    {
        var license = CreateBetaLicense(customerEmail, customerName, installationId, days);
        var signed = Sign(license);
        return WriteLicenseFile(signed, license.LicenseId, outputDirectory);
    }

    public LicenseValidationResult VerifyFile(
        string licenseFilePath,
        string installationId,
        DateTimeOffset? currentUtc = null)
    {
        var json = File.ReadAllText(licenseFilePath);
        var signed = JsonSerializer.Deserialize<SignedLicense>(json)
            ?? throw new InvalidOperationException("Invalid license file.");

        var trustedKeys = new Dictionary<string, string>
        {
            [_keyId] = _keys.ReadPublicKeyPem()
        };

        var validator = new RsaLicenseValidator(trustedKeys);
        return validator.Validate(
            signed,
            installationId,
            currentUtc ?? DateTimeOffset.UtcNow);
    }
}
