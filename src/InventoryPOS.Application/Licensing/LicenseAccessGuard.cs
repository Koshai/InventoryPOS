
namespace InventoryPOS.Application.Licensing;

public sealed class LicenseAccessGuard : ILicenseAccessGuard
{
    private readonly ILicenseActivationService _activation;

    public LicenseAccessGuard(
        ILicenseActivationService activation)
    {
        _activation = activation;
    }

    public void EnsureCanWrite()
    {
        var result = _activation.GetCurrentLicenseStatus();

        if (result.IsUsable)
            return;

        throw new InvalidOperationException(
            $"InventoryPOS cannot perform this operation. " +
            $"License status: {result.Status}. " +
            result.Message);
    }
}
