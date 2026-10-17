
using System.Security.Cryptography;
using System.Text.Json;
using InventoryPOS.Application.Licensing;

namespace InventoryPOS.Tests.Licensing;

public sealed class SignedLicenseIntegrationTests
{
    private const string TestKeyId = "integration-test-key";

    [Fact]
    public void ValidSignedBetaLicense_PassesValidation()
    {
        using var rsa = RSA.Create(3072);
        var validator = CreateValidator(rsa);

        var now = DateTimeOffset.UtcNow;
        var document = CreateDocument(now);

        var signed = Sign(rsa, document);

        var result = validator.Validate(
            signed,
            document.InstallationId,
            now);

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.True(result.IsUsable);
        Assert.Equal(document.LicenseId, result.License?.LicenseId);
    }

    [Fact]
    public void ModifiedPayload_FailsSignatureValidation()
    {
        using var rsa = RSA.Create(3072);
        var validator = CreateValidator(rsa);

        var now = DateTimeOffset.UtcNow;
        var document = CreateDocument(now);
        var signed = Sign(rsa, document);

        // Modify the payload without creating a new signature.
        document.CustomerName = "Unauthorized Customer";
        signed.PayloadBase64 = Convert.ToBase64String(
            JsonSerializer.SerializeToUtf8Bytes(document));

        var result = validator.Validate(
            signed,
            document.InstallationId,
            now);

        Assert.Equal(LicenseStatus.InvalidSignature, result.Status);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public void ValidSignature_WrongInstallation_IsRejected()
    {
        using var rsa = RSA.Create(3072);
        var validator = CreateValidator(rsa);

        var now = DateTimeOffset.UtcNow;
        var document = CreateDocument(now);
        var signed = Sign(rsa, document);

        var result = validator.Validate(
            signed,
            "another-installation",
            now);

        Assert.Equal(LicenseStatus.WrongInstallation, result.Status);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public void ValidSignature_ExpiredLicense_IsRejected()
    {
        using var rsa = RSA.Create(3072);
        var validator = CreateValidator(rsa);

        var now = DateTimeOffset.UtcNow;
        var document = CreateDocument(now);
        document.ExpiresAtUtc = now.AddMinutes(-1);

        var signed = Sign(rsa, document);

        var result = validator.Validate(
            signed,
            document.InstallationId,
            now);

        Assert.Equal(LicenseStatus.Expired, result.Status);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public void ValidSignature_ExpiringSoon_IsUsable()
    {
        using var rsa = RSA.Create(3072);
        var validator = CreateValidator(rsa);

        var now = DateTimeOffset.UtcNow;
        var document = CreateDocument(now);
        document.ExpiresAtUtc = now.AddDays(3);

        var signed = Sign(rsa, document);

        var result = validator.Validate(
            signed,
            document.InstallationId,
            now);

        Assert.Equal(LicenseStatus.ExpiringSoon, result.Status);
        Assert.True(result.IsUsable);
    }

    private static RsaLicenseValidator CreateValidator(RSA rsa)
    {
        var keys = new Dictionary<string, string>
        {
            [TestKeyId] = rsa.ExportSubjectPublicKeyInfoPem()
        };

        return new RsaLicenseValidator(keys);
    }

    private static LicenseDocument CreateDocument(DateTimeOffset now)
    {
        return new LicenseDocument
        {
            SchemaVersion = 1,
            LicenseId = Guid.NewGuid(),
            CustomerId = "test-customer",
            CustomerName = "Integration Test Customer",
            Edition = LicenseEdition.Beta,
            InstallationId = Guid.NewGuid().ToString("D"),
            IssuedAtUtc = now.AddDays(-1),
            ValidFromUtc = now.AddDays(-1),
            ExpiresAtUtc = now.AddDays(30),
            MaxInstallations = 1
        };
    }

    private static SignedLicense Sign(
        RSA rsa,
        LicenseDocument document)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(document);

        var signature = rsa.SignData(
            payload,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        return new SignedLicense
        {
            KeyId = TestKeyId,
            PayloadBase64 = Convert.ToBase64String(payload),
            SignatureBase64 = Convert.ToBase64String(signature)
        };
    }
}
