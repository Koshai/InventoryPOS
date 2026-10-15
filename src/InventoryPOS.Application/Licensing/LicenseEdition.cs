
namespace InventoryPOS.Application.Licensing;

public enum LicenseEdition
{
    Beta = 1,
    Standard = 2,
    Professional = 3
}

public enum LicenseStatus
{
    Valid = 1,
    ExpiringSoon = 2,
    Expired = 3,
    NotYetValid = 4,
    InvalidSignature = 5,
    WrongInstallation = 6,
    InvalidLicense = 7
}
