
using InventoryPOS.Application.Licensing;
using System;
using System.IO;
using System.Text.Json;

namespace InventoryPOS.Desktop.Services;

public sealed class FileLicenseActivationService
    : ILicenseActivationService
{
    private readonly IInstallationIdentityService _identityService;
    private readonly ILicenseValidator _validator;
    private readonly string _licensePath;

    public FileLicenseActivationService(
        IInstallationIdentityService identityService,
        ILicenseValidator validator)
    {
        _identityService = identityService;
        _validator = validator;

        string directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "Licensing");

        _licensePath = Path.Combine(directory, "license.iposlic");
    }

    public LicenseValidationResult GetCurrentLicenseStatus()
    {
        if (!File.Exists(_licensePath))
        {
            return Failure("No license has been activated.");
        }

        try
        {
            var signedLicense = ReadLicense(_licensePath);

            return _validator.Validate(
                signedLicense,
                _identityService.GetOrCreateInstallationId(),
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException)
        {
            return Failure("The installed license could not be read.");
        }
    }

    public LicenseValidationResult ActivateFromFile(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Failure("Select a license file.");
        }

        SignedLicense signedLicense;

        try
        {
            signedLicense = ReadLicense(sourcePath);
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException)
        {
            return Failure("The selected license file is invalid.");
        }

        var result = _validator.Validate(
            signedLicense,
            _identityService.GetOrCreateInstallationId(),
            DateTimeOffset.UtcNow);

        if (!result.IsUsable)
        {
            return result;
        }

        string directory = Path.GetDirectoryName(_licensePath)!;
        Directory.CreateDirectory(directory);

        string temporaryPath = Path.Combine(
            directory,
            $".license-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(signedLicense));

            File.Move(temporaryPath, _licensePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return result;
    }

    private static SignedLicense ReadLicense(string path)
    {
        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<SignedLicense>(json)
            ?? throw new JsonException("Empty license document.");
    }

    private static LicenseValidationResult Failure(string message)
    {
        return new LicenseValidationResult
        {
            Status = LicenseStatus.InvalidLicense,
            Message = message
        };
    }
}
