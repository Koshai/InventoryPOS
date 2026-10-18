namespace InventoryPOS.Application.Licensing;

public static class LicensePolicy
{
    public static LicenseValidationResult Evaluate(
        LicenseDocument license,
        string installationId,
        DateTimeOffset currentUtc)
    {
        if (!IsStructurallyValid(license))
        {
            return Result(
                LicenseStatus.InvalidLicense,
                license,
                "The license contains invalid information.");
        }

        if (!string.Equals(
                license.InstallationId,
                installationId,
                StringComparison.Ordinal))
        {
            return Result(
                LicenseStatus.WrongInstallation,
                license,
                "This license belongs to another installation.");
        }

        if (currentUtc < license.ValidFromUtc)
        {
            return Result(
                LicenseStatus.NotYetValid,
                license,
                "This license is not yet active.");
        }

        if (license.ExpiresAtUtc.HasValue &&
            currentUtc >= license.ExpiresAtUtc.Value)
        {
            return Result(
                LicenseStatus.Expired,
                license,
                "The license has expired.");
        }

        if (license.ExpiresAtUtc.HasValue &&
            license.ExpiresAtUtc.Value - currentUtc <=
            TimeSpan.FromDays(7))
        {
            return Result(
                LicenseStatus.ExpiringSoon,
                license,
                "The license will expire within seven days.");
        }

        return Result(
            LicenseStatus.Valid,
            license,
            "The license is valid.");
    }

    private static bool IsStructurallyValid(LicenseDocument license)
    {
        if (license.LicenseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(license.CustomerId) ||
            string.IsNullOrWhiteSpace(license.CustomerName) ||
            string.IsNullOrWhiteSpace(license.InstallationId) ||
            license.MaxInstallations < 1 ||
            !Enum.IsDefined(license.Edition) ||
            license.IssuedAtUtc == default ||
            license.ValidFromUtc == default ||
            license.ValidFromUtc < license.IssuedAtUtc ||
            (license.Edition == LicenseEdition.Beta &&
            !license.ExpiresAtUtc.HasValue) ||
            (license.ExpiresAtUtc.HasValue &&
            license.ExpiresAtUtc.Value <= license.ValidFromUtc))
        {
            return false;
        }

        if (license.SchemaVersion == 1)
            return true;

        if (license.SchemaVersion == 2)
        {
            var email = license.ResolveCustomerEmail();
            return !string.IsNullOrWhiteSpace(email) &&
                   email.Contains('@');
        }

        return false;
    }

    private static LicenseValidationResult Result(
        LicenseStatus status,
        LicenseDocument license,
        string message)
    {
        return new LicenseValidationResult
        {
            Status = status,
            License = license,
            Message = message
        };
    }
}
