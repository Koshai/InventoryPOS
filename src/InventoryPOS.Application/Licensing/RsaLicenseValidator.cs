
using System.Security.Cryptography;
using System.Text.Json;

namespace InventoryPOS.Application.Licensing;

public sealed class RsaLicenseValidator : ILicenseValidator
{
    private readonly IReadOnlyDictionary<string, string> _trustedPublicKeys;

    public RsaLicenseValidator(
        IReadOnlyDictionary<string, string> trustedPublicKeys)
    {
        _trustedPublicKeys = trustedPublicKeys
            ?? throw new ArgumentNullException(nameof(trustedPublicKeys));
    }

    public LicenseValidationResult Validate(
        SignedLicense signedLicense,
        string installationId,
        DateTimeOffset currentUtc)
    {
        if (signedLicense is null ||
            string.IsNullOrWhiteSpace(signedLicense.KeyId) ||
            string.IsNullOrWhiteSpace(signedLicense.PayloadBase64) ||
            string.IsNullOrWhiteSpace(signedLicense.SignatureBase64))
        {
            return Invalid(
                LicenseStatus.InvalidLicense,
                "The license file is missing required information.");
        }

        if (!_trustedPublicKeys.TryGetValue(
                signedLicense.KeyId,
                out var publicKeyPem))
        {
            return Invalid(
                LicenseStatus.InvalidSignature,
                "The license was signed with an unknown key.");
        }

        try
        {
            byte[] payload =
                Convert.FromBase64String(signedLicense.PayloadBase64);

            byte[] signature =
                Convert.FromBase64String(signedLicense.SignatureBase64);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            bool verified = rsa.VerifyData(
                payload,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            if (!verified)
            {
                return Invalid(
                    LicenseStatus.InvalidSignature,
                    "The license signature is invalid.");
            }

            var license = JsonSerializer.Deserialize<LicenseDocument>(
                payload);

            if (license is null)
            {
                return Invalid(
                    LicenseStatus.InvalidLicense,
                    "The license payload is invalid.");
            }

            return LicensePolicy.Evaluate(
                license,
                installationId,
                currentUtc);
        }
        catch (Exception ex) when (
            ex is FormatException ||
            ex is JsonException ||
            ex is CryptographicException ||
            ex is ArgumentException)
        {
            return Invalid(
                LicenseStatus.InvalidLicense,
                "The license file could not be validated.");
        }
    }

    private static LicenseValidationResult Invalid(
        LicenseStatus status,
        string message)
    {
        return new LicenseValidationResult
        {
            Status = status,
            Message = message
        };
    }
}
