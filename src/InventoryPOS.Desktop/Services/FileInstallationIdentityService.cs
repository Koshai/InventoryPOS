
using InventoryPOS.Application.Licensing;
using System;
using System.IO;
using System.Security.Cryptography;

namespace InventoryPOS.Desktop.Services;

public sealed class FileInstallationIdentityService
    : IInstallationIdentityService
{
    private readonly string _identityPath;

    public FileInstallationIdentityService()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "Licensing");

        _identityPath = Path.Combine(
            directory,
            "installation.id");
    }

    public string GetOrCreateInstallationId()
    {
        string directory = Path.GetDirectoryName(_identityPath)!;

        Directory.CreateDirectory(directory);

        if (File.Exists(_identityPath))
        {
            string existingId =
                File.ReadAllText(_identityPath).Trim();

            if (Guid.TryParse(existingId, out var parsedId) &&
                parsedId != Guid.Empty)
            {
                return parsedId.ToString("D");
            }

            throw new InvalidOperationException(
                "The existing installation identity is invalid.");
        }

        string installationId = Guid.NewGuid().ToString("D");

        // CreateNew prevents accidentally overwriting an
        // identity created by another process.
        using (var stream = new FileStream(
            _identityPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(installationId);
        }

        return installationId;
    }
}
