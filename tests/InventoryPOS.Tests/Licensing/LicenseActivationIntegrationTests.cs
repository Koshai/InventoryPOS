
using System.Security.Cryptography;
using System.Text.Json;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Desktop.Services;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicenseActivationIntegrationTests
{
    [Fact]
    public void ValidLicense_ActivatesAndPermitsWrites()
    {
        using var test = new TestEnvironment();

        var license = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddDays(30));

        var result = test.Activation.ActivateFromFile(license);

        Assert.True(result.IsUsable);
        Assert.Equal(LicenseStatus.Valid, result.Status);

        var guard = new LicenseAccessGuard(test.Activation);

        guard.EnsureCanWrite();

        Assert.True(File.Exists(test.InstalledLicensePath));
    }

    [Fact]
    public void ExpiredLicense_CannotBeActivated()
    {
        using var test = new TestEnvironment();

        var license = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = test.Activation.ActivateFromFile(license);

        Assert.Equal(LicenseStatus.Expired, result.Status);
        Assert.False(result.IsUsable);
        Assert.False(File.Exists(test.InstalledLicensePath));

        var guard = new LicenseAccessGuard(test.Activation);

        Assert.Throws<InvalidOperationException>(
            () => guard.EnsureCanWrite());
    }

    [Fact]
    public void WrongInstallation_CannotBeActivated()
    {
        using var test = new TestEnvironment();

        var license = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddDays(30),
            installationId: "another-installation");

        var result = test.Activation.ActivateFromFile(license);

        Assert.Equal(LicenseStatus.WrongInstallation, result.Status);
        Assert.False(result.IsUsable);
        Assert.False(File.Exists(test.InstalledLicensePath));
    }

    [Fact]
    public void TamperedLicense_CannotReplaceValidLicense()
    {
        using var test = new TestEnvironment();

        var valid = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddDays(30));

        Assert.True(test.Activation.ActivateFromFile(valid).IsUsable);

        var originalInstalled =
            File.ReadAllText(test.InstalledLicensePath);

        var tamperedPath = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddDays(30));

        var tampered = JsonSerializer.Deserialize<SignedLicense>(
            File.ReadAllText(tamperedPath))!;

        var payload = Convert.FromBase64String(
            tampered.PayloadBase64);

        payload[0] ^= 1;

        tampered.PayloadBase64 =
            Convert.ToBase64String(payload);

        File.WriteAllText(
            tamperedPath,
            JsonSerializer.Serialize(tampered));

        var result = test.Activation.ActivateFromFile(tamperedPath);

        Assert.Equal(LicenseStatus.InvalidSignature, result.Status);

        Assert.Equal(
            originalInstalled,
            File.ReadAllText(test.InstalledLicensePath));

        new LicenseAccessGuard(test.Activation).EnsureCanWrite();
    }

    [Fact]
    public void InstalledLicense_TamperingBlocksWrites()
    {
        using var test = new TestEnvironment();

        var valid = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddDays(30));

        Assert.True(test.Activation.ActivateFromFile(valid).IsUsable);

        var signed = JsonSerializer.Deserialize<SignedLicense>(
            File.ReadAllText(test.InstalledLicensePath))!;

        signed.SignatureBase64 =
            Convert.ToBase64String(new byte[384]);

        File.WriteAllText(
            test.InstalledLicensePath,
            JsonSerializer.Serialize(signed));

        Assert.Throws<InvalidOperationException>(
            () => new LicenseAccessGuard(
                test.Activation).EnsureCanWrite());
    }


    [Fact]
    public void InstalledLicense_ExpiresAfterActivation_BlocksWrites()
    {
        using var test = new TestEnvironment();

        // Activate a license that is currently valid.
        var license = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddDays(30));

        var activationResult =
            test.Activation.ActivateFromFile(license);

        Assert.Equal(
            LicenseStatus.Valid,
            activationResult.Status);

        var guard = new LicenseAccessGuard(test.Activation);

        // A valid activated license permits writes.
        guard.EnsureCanWrite();

        // Simulate the passage of time by replacing the
        // installed license with a correctly signed,
        // but already expired, license.
        //
        // This models an installed license that has
        // reached its expiry date.

        var expiredLicense = test.CreateSignedLicense(
            DateTimeOffset.UtcNow.AddMinutes(-1));

        File.Copy(
            expiredLicense,
            test.InstalledLicensePath,
            overwrite: true);

        // The activation service must revalidate the
        // installed license when checking its status.
        var currentStatus =
            test.Activation.GetCurrentLicenseStatus();

        Assert.Equal(
            LicenseStatus.Expired,
            currentStatus.Status);

        Assert.False(currentStatus.IsUsable);

        // The same guard instance must now reject writes.
        var exception =
            Assert.Throws<InvalidOperationException>(
                () => guard.EnsureCanWrite());

        Assert.Contains(
            "Expired",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }


    private sealed class TestEnvironment : IDisposable
    {
        private const string KeyId = "activation-test-key";

        private readonly RSA _rsa = RSA.Create(3072);
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(),
            "InventoryPOS-LicenseTests",
            Guid.NewGuid().ToString("N"));

        private readonly string _installationId =
            Guid.NewGuid().ToString("D");

        public string InstalledLicensePath =>
            Path.Combine(_directory, "installed", "license.iposlic");

        public FileLicenseActivationService Activation { get; }

        public TestEnvironment()
        {
            Directory.CreateDirectory(_directory);

            var keys = new Dictionary<string, string>
            {
                [KeyId] = _rsa.ExportSubjectPublicKeyInfoPem()
            };

            Activation = new FileLicenseActivationService(
                new FixedInstallationIdentity(_installationId),
                new RsaLicenseValidator(keys),
                Path.Combine(_directory, "installed"));
        }

        public string CreateSignedLicense(
            DateTimeOffset expiresAt,
            string? installationId = null)
        {
            var now = DateTimeOffset.UtcNow;

            var document = new LicenseDocument
            {
                SchemaVersion = 1,
                LicenseId = Guid.NewGuid(),
                CustomerId = "activation-test-customer",
                CustomerName = "Activation Test Customer",
                Edition = LicenseEdition.Beta,
                InstallationId =
                    installationId ?? _installationId,
                IssuedAtUtc = now.AddDays(-2),
                ValidFromUtc = now.AddDays(-2),
                ExpiresAtUtc = expiresAt,
                MaxInstallations = 1
            };

            var payload =
                JsonSerializer.SerializeToUtf8Bytes(document);

            var signature = _rsa.SignData(
                payload,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            var signed = new SignedLicense
            {
                KeyId = KeyId,
                PayloadBase64 =
                    Convert.ToBase64String(payload),
                SignatureBase64 =
                    Convert.ToBase64String(signature)
            };

            var path = Path.Combine(
                _directory,
                $"source-{Guid.NewGuid():N}.iposlic");

            File.WriteAllText(
                path,
                JsonSerializer.Serialize(signed));

            return path;
        }

        public void Dispose()
        {
            _rsa.Dispose();

            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class FixedInstallationIdentity
        : IInstallationIdentityService
    {
        private readonly string _installationId;

        public FixedInstallationIdentity(string installationId)
        {
            _installationId = installationId;
        }

        public string GetOrCreateInstallationId() =>
            _installationId;
    }
}
