using System.Text.Json;
using InventoryPOS.Application.Licensing;
using InventoryPOS.LicensePortal.Data;
using InventoryPOS.LicensePortal.Options;
using InventoryPOS.Licensing.Issuer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryPOS.LicensePortal.Services;

public sealed class LicenseIssueService
{
    private readonly ApplicationDbContext _db;
    private readonly LicenseIssuerService _issuer;
    private readonly PortalOptions _options;

    public LicenseIssueService(
        ApplicationDbContext db,
        LicenseIssuerService issuer,
        IOptions<PortalOptions> options)
    {
        _db = db;
        _issuer = issuer;
        _options = options.Value;
    }

    public async Task<(MachineLicense Machine, string FilePath)> IssueOrRenewAsync(
        string email,
        string displayName,
        string installationId,
        int days,
        string issuedByUserName,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName.Trim();
        installationId = installationId.Trim();
        notes ??= string.Empty;

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Email = email,
                DisplayName = displayName,
                Notes = string.Empty,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (!string.Equals(customer.DisplayName, displayName, StringComparison.Ordinal))
        {
            customer.DisplayName = displayName;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var document = _issuer.CreateBetaLicense(email, displayName, installationId, days);
        var signed = _issuer.Sign(document);
        var filePath = _issuer.WriteLicenseFile(
            signed,
            document.LicenseId,
            _options.IssuedLicensesDirectory);
        var fileName = Path.GetFileName(filePath);
        var featuresJson = JsonSerializer.Serialize(document.Features);

        var machine = await _db.MachineLicenses
            .FirstOrDefaultAsync(
                m => m.CustomerId == customer.Id && m.InstallationId == installationId,
                cancellationToken);

        if (machine is null)
        {
            machine = new MachineLicense
            {
                CustomerId = customer.Id,
                InstallationId = installationId
            };
            _db.MachineLicenses.Add(machine);
        }

        machine.Edition = document.Edition.ToString();
        machine.LicenseId = document.LicenseId;
        machine.IssuedAtUtc = document.IssuedAtUtc;
        machine.ValidFromUtc = document.ValidFromUtc;
        machine.ExpiresAtUtc = document.ExpiresAtUtc!.Value;
        machine.FeaturesJson = featuresJson;
        machine.LastIssuedFileName = fileName;

        _db.IssueHistories.Add(new IssueHistory
        {
            MachineLicense = machine,
            IssuedAtUtc = document.IssuedAtUtc,
            ExpiresAtUtc = document.ExpiresAtUtc!.Value,
            FileName = fileName,
            Notes = notes,
            IssuedByUserName = issuedByUserName
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (machine, filePath);
    }

    public string GetIssuedLicensePath(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        var path = Path.Combine(_options.IssuedLicensesDirectory, safe);
        if (!File.Exists(path))
            throw new FileNotFoundException("Issued license file not found.", path);
        return path;
    }
}
