using InventoryPOS.Licensing.Issuer;

// Optional advanced CLI. Prefer InventoryPOS.LicenseManager for day-to-day use.

if (args.Length == 0)
{
    ShowUsage();
    return;
}

var keys = new LicenseKeyStore();
keys.EnsureMigratedFromLegacyAppData();
var issuer = new LicenseIssuerService(keys);

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "generate-keys":
            keys.GenerateKeys();
            Console.WriteLine("RSA key pair generated.");
            Console.WriteLine($"Private key: {keys.PrivateKeyPath}");
            Console.WriteLine($"Public key:  {keys.PublicKeyPath}");
            Console.WriteLine();
            Console.WriteLine(
                "IMPORTANT: Back up the private key securely. Never distribute it.");
            Console.WriteLine(
                "Embed the public key into InventoryPOS.Desktop TrustedLicenseKeys before shipping.");
            break;

        case "issue-beta":
            IssueBeta(args, issuer);
            break;

        case "verify":
            VerifyLicense(args, issuer);
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

static void IssueBeta(string[] args, LicenseIssuerService issuer)
{
    if (args.Length != 5)
    {
        Console.WriteLine(
            "Usage: issue-beta <customer-email> <customer-name> " +
            "<installation-id> <days>");
        return;
    }

    if (!int.TryParse(args[4], out int days))
        throw new ArgumentException("Invalid duration.");

    var outputDirectory = Path.Combine(
        Environment.CurrentDirectory,
        "issued-licenses");

    var outputPath = issuer.IssueBetaToFile(
        args[1],
        args[2],
        args[3],
        days,
        outputDirectory);

    Console.WriteLine("Beta license issued.");
    Console.WriteLine($"Customer email: {args[1]}");
    Console.WriteLine($"License:  {outputPath}");
}

static void VerifyLicense(string[] args, LicenseIssuerService issuer)
{
    if (args.Length != 3)
    {
        throw new ArgumentException(
            "Usage: verify <license-file> <installation-id>");
    }

    var result = issuer.VerifyFile(args[1], args[2]);
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Message: {result.Message}");

    if (!result.IsUsable)
        Environment.ExitCode = 1;
}

static void ShowUsage()
{
    Console.WriteLine("InventoryPOS License Issuer (advanced CLI)");
    Console.WriteLine("Prefer the License Manager desktop app for day-to-day use.");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  generate-keys");
    Console.WriteLine(
        "  issue-beta <customer-email> <customer-name> " +
        "<installation-id> <days>");
    Console.WriteLine(
        "  verify <license-file> <installation-id>");
}
