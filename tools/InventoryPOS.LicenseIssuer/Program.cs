using System.Security.Cryptography;
using System.Text.Json;
using InventoryPOS.Application.Licensing;

const string keyId = "inventorypos-2026-01";

if (args.Length == 0)
{
    ShowUsage();
    return;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "generate-keys":
            GenerateKeys();
            break;

        case "issue-beta":
            IssueBeta(args);
            break;

        default:
            ShowUsage();
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
}

static void GenerateKeys()
{
    string directory = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData),
        "InventoryPOS",
        "LicenseIssuer");

    Directory.CreateDirectory(directory);

    string privatePath = Path.Combine(
        directory, "private-key.pem");

    string publicPath = Path.Combine(
        directory, "public-key.pem");

    if (File.Exists(privatePath) ||
        File.Exists(publicPath))
    {
        throw new InvalidOperationException(
            "Key files already exist. Refusing to overwrite them.");
    }

    using var rsa = RSA.Create(3072);

    File.WriteAllText(
        privatePath,
        rsa.ExportPkcs8PrivateKeyPem());

    File.WriteAllText(
        publicPath,
        rsa.ExportSubjectPublicKeyInfoPem());

    Console.WriteLine("RSA key pair generated.");
    Console.WriteLine($"Private key: {privatePath}");
    Console.WriteLine($"Public key:  {publicPath}");
    Console.WriteLine();
    Console.WriteLine(
        "IMPORTANT: Back up the private key securely. Never distribute it.");
}

static void IssueBeta(string[] args)
{
    if (args.Length != 5)
    {
        Console.WriteLine(
            "Usage: issue-beta <customer-id> <customer-name> " +
            "<installation-id> <days>");
        return;
    }

    string customerId = args[1];
    string customerName = args[2];
    string installationId = args[3];

    if (string.IsNullOrWhiteSpace(customerId) ||
        string.IsNullOrWhiteSpace(customerName) ||
        string.IsNullOrWhiteSpace(installationId) ||
        !int.TryParse(args[4], out int days) ||
        days < 1 || days > 365)
    {
        throw new ArgumentException(
            "Invalid customer, installation ID, or duration.");
    }

    string privatePath = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData),
        "InventoryPOS",
        "LicenseIssuer",
        "private-key.pem");

    if (!File.Exists(privatePath))
    {
        throw new FileNotFoundException(
            "Private signing key not found. Generate keys first.");
    }

    var now = DateTimeOffset.UtcNow;

    var license = new LicenseDocument
    {
        SchemaVersion = 1,
        LicenseId = Guid.NewGuid(),
        CustomerId = customerId,
        CustomerName = customerName,
        Edition = LicenseEdition.Beta,
        InstallationId = installationId,
        IssuedAtUtc = now,
        ValidFromUtc = now,
        ExpiresAtUtc = now.AddDays(days),
        MaxInstallations = 1,
        Features = new List<string>
        {
            "POS",
            "INVENTORY",
            "PURCHASING"
        }
    };

    byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
        license);

    using var rsa = RSA.Create();

    rsa.ImportFromPem(File.ReadAllText(privatePath));

    byte[] signature = rsa.SignData(
        payload,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pss);

    var signedLicense = new SignedLicense
    {
        KeyId = keyId,
        PayloadBase64 = Convert.ToBase64String(payload),
        SignatureBase64 = Convert.ToBase64String(signature)
    };

    string outputDirectory = Path.Combine(
        Environment.CurrentDirectory,
        "issued-licenses");

    Directory.CreateDirectory(outputDirectory);

    string outputPath = Path.Combine(
        outputDirectory,
        $"{license.LicenseId:N}.iposlic");

    File.WriteAllText(
        outputPath,
        JsonSerializer.Serialize(
            signedLicense,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }));

    Console.WriteLine("Beta license issued.");
    Console.WriteLine($"Customer: {customerName}");
    Console.WriteLine($"Expires:  {license.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC");
    Console.WriteLine($"License:  {outputPath}");
}

static void ShowUsage()
{
    Console.WriteLine("InventoryPOS License Issuer");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  generate-keys");
    Console.WriteLine(
        "  issue-beta <customer-id> <customer-name> " +
        "<installation-id> <days>");
}
