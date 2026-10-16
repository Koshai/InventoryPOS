
namespace InventoryPOS.Application.Licensing;

public interface IInstallationIdentityService
{
    string GetOrCreateInstallationId();
}
