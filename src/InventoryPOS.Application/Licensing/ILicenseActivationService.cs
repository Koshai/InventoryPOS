
namespace InventoryPOS.Application.Licensing;

public interface ILicenseActivationService
{
    LicenseValidationResult GetCurrentLicenseStatus();

    LicenseValidationResult ActivateFromFile(string sourcePath);
}
