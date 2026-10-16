
namespace InventoryPOS.Application.Licensing;

public interface ILicenseAccessGuard
{
    void EnsureCanWrite();
}
