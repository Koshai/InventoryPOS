using System.Security.Cryptography;

namespace InventoryPOS.Licensing.Issuer;

/// <summary>
/// Stores RSA signing keys under ProgramData (with one-time migration from AppData).
/// </summary>
public sealed class LicenseKeyStore
{
    public const string DefaultKeyId = "inventorypos-2026-01";

    private readonly string _directory;

    public LicenseKeyStore()
        : this(ResolveDefaultDirectory())
    {
    }

    public LicenseKeyStore(string directory)
    {
        _directory = directory;
    }

    public string DirectoryPath => _directory;

    public string PrivateKeyPath => Path.Combine(_directory, "private-key.pem");

    public string PublicKeyPath => Path.Combine(_directory, "public-key.pem");

    public bool HasKeys =>
        File.Exists(PrivateKeyPath) && File.Exists(PublicKeyPath);

    public void EnsureMigratedFromLegacyAppData()
    {
        if (HasKeys)
            return;

        var legacyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InventoryPOS",
            "LicenseIssuer");

        var legacyPrivate = Path.Combine(legacyDirectory, "private-key.pem");
        var legacyPublic = Path.Combine(legacyDirectory, "public-key.pem");

        if (!File.Exists(legacyPrivate) || !File.Exists(legacyPublic))
            return;

        Directory.CreateDirectory(_directory);
        File.Copy(legacyPrivate, PrivateKeyPath, overwrite: false);
        File.Copy(legacyPublic, PublicKeyPath, overwrite: false);
    }

    public void GenerateKeys()
    {
        Directory.CreateDirectory(_directory);

        if (File.Exists(PrivateKeyPath) || File.Exists(PublicKeyPath))
        {
            throw new InvalidOperationException(
                "Key files already exist. Refusing to overwrite them. " +
                "Back up and remove the existing keys first if you intentionally want a new pair.");
        }

        using var rsa = RSA.Create(3072);
        File.WriteAllText(PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(PublicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
    }

    public string ReadPrivateKeyPem()
    {
        EnsureMigratedFromLegacyAppData();
        if (!File.Exists(PrivateKeyPath))
            throw new FileNotFoundException("Private signing key not found. Generate keys first.", PrivateKeyPath);
        return File.ReadAllText(PrivateKeyPath);
    }

    public string ReadPublicKeyPem()
    {
        EnsureMigratedFromLegacyAppData();
        if (!File.Exists(PublicKeyPath))
            throw new FileNotFoundException("Public verification key not found.", PublicKeyPath);
        return File.ReadAllText(PublicKeyPath);
    }

    private static string ResolveDefaultDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "LicenseIssuer");
}
