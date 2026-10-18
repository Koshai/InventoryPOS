
using InventoryPOS.Application.Licensing;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicensePolicyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ValidBetaLicense_IsUsable()
    {
        var license = CreateLicense();

        var result = LicensePolicy.Evaluate(
            license, "TEST-PC", Now);

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.True(result.IsUsable);
    }

    [Fact]
    public void ExpiringSoonLicense_RemainsUsable()
    {
        var license = CreateLicense();
        license.ExpiresAtUtc = Now.AddDays(3);

        var result = LicensePolicy.Evaluate(
            license, "TEST-PC", Now);

        Assert.Equal(
            LicenseStatus.ExpiringSoon, result.Status);
        Assert.True(result.IsUsable);
    }

    [Fact]
    public void ExpiredLicense_IsNotUsable()
    {
        var license = CreateLicense();
        license.ExpiresAtUtc = Now.AddMinutes(-1);

        var result = LicensePolicy.Evaluate(
            license, "TEST-PC", Now);

        Assert.Equal(LicenseStatus.Expired, result.Status);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public void WrongInstallation_IsRejected()
    {
        var license = CreateLicense();

        var result = LicensePolicy.Evaluate(
            license, "OTHER-PC", Now);

        Assert.Equal(
            LicenseStatus.WrongInstallation, result.Status);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public void BetaWithoutExpiration_IsRejected()
    {
        var license = CreateLicense();
        license.ExpiresAtUtc = null;

        var result = LicensePolicy.Evaluate(
            license, "TEST-PC", Now);

        Assert.Equal(
            LicenseStatus.InvalidLicense, result.Status);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public void Schema2_RequiresCustomerEmail()
    {
        var license = CreateLicenseSchema2();
        license.CustomerEmail = string.Empty;
        license.CustomerId = "NOT-AN-EMAIL";

        var result = LicensePolicy.Evaluate(
            license, "TEST-PC", Now);

        Assert.Equal(
            LicenseStatus.InvalidLicense, result.Status);
    }

    [Fact]
    public void Schema2_ValidEmailLicense_IsUsable()
    {
        var license = CreateLicenseSchema2();

        var result = LicensePolicy.Evaluate(
            license, "TEST-PC", Now);

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.Equal("shop@example.com", license.ResolveCustomerEmail());
    }

    private static LicenseDocument CreateLicense()
    {
        return new LicenseDocument
        {
            SchemaVersion = 1,
            LicenseId = Guid.NewGuid(),
            CustomerId = "TEST-001",
            CustomerName = "Test Customer",
            Edition = LicenseEdition.Beta,
            InstallationId = "TEST-PC",
            IssuedAtUtc = Now.AddDays(-1),
            ValidFromUtc = Now.AddDays(-1),
            ExpiresAtUtc = Now.AddDays(29),
            MaxInstallations = 1
        };
    }

    private static LicenseDocument CreateLicenseSchema2()
    {
        return new LicenseDocument
        {
            SchemaVersion = 2,
            LicenseId = Guid.NewGuid(),
            CustomerId = "shop@example.com",
            CustomerEmail = "shop@example.com",
            CustomerName = "Example Shop",
            Edition = LicenseEdition.Beta,
            InstallationId = "TEST-PC",
            IssuedAtUtc = Now.AddDays(-1),
            ValidFromUtc = Now.AddDays(-1),
            ExpiresAtUtc = Now.AddDays(29),
            MaxInstallations = 1
        };
    }
}
