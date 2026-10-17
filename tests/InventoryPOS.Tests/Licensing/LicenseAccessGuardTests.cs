
using InventoryPOS.Application.Licensing;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicenseAccessGuardTests
{
    [Theory]
    [InlineData(LicenseStatus.Valid)]
    [InlineData(LicenseStatus.ExpiringSoon)]
    public void UsableLicense_AllowsWrite(
        LicenseStatus status)
    {
        var activation = new FakeActivationService(status);
        var guard = new LicenseAccessGuard(activation);

        guard.EnsureCanWrite();

        Assert.Equal(1, activation.StatusCheckCount);
    }

    [Theory]
    [InlineData(LicenseStatus.Expired)]
    [InlineData(LicenseStatus.NotYetValid)]
    [InlineData(LicenseStatus.InvalidSignature)]
    [InlineData(LicenseStatus.WrongInstallation)]
    [InlineData(LicenseStatus.InvalidLicense)]
    public void UnusableLicense_BlocksWrite(
        LicenseStatus status)
    {
        var activation = new FakeActivationService(status);
        var guard = new LicenseAccessGuard(activation);

        var exception = Assert.Throws<InvalidOperationException>(
            () => guard.EnsureCanWrite());

        Assert.Contains(status.ToString(), exception.Message);
        Assert.Equal(1, activation.StatusCheckCount);
    }

    [Fact]
    public void Guard_RechecksLicenseOnEveryOperation()
    {
        var activation =
            new FakeActivationService(LicenseStatus.Valid);

        var guard = new LicenseAccessGuard(activation);

        guard.EnsureCanWrite();

        activation.Status = LicenseStatus.Expired;

        Assert.Throws<InvalidOperationException>(
            () => guard.EnsureCanWrite());

        Assert.Equal(2, activation.StatusCheckCount);
    }

    private sealed class FakeActivationService
        : ILicenseActivationService
    {
        public LicenseStatus Status { get; set; }

        public int StatusCheckCount { get; private set; }

        public FakeActivationService(LicenseStatus status)
        {
            Status = status;
        }

        public LicenseValidationResult GetCurrentLicenseStatus()
        {
            StatusCheckCount++;

            return new LicenseValidationResult
            {
                Status = Status,
                Message = "Test license status."
            };
        }

        public LicenseValidationResult ActivateFromFile(
            string sourcePath)
        {
            throw new NotSupportedException(
                "Activation is not required for this test.");
        }
    }
}
