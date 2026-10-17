using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Desktop.Services;

/// <summary>
/// One unresolved checkout per workstation.
/// Stored under Storage:DataRoot (not MSIX LocalCache) so operators can find it.
/// The server database remains the authority for whether a sale committed.
/// </summary>
public sealed class PendingCheckoutJournalService : IPendingCheckoutJournalService
{
    private const string FileName = "pending-checkout.json";

    private readonly string _path;
    private readonly string? _legacyPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public PendingCheckoutJournalService()
        : this(ResolveDefaultDataRoot())
    {
    }

    public PendingCheckoutJournalService(string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("Data root is required.", nameof(dataRoot));

        var directory = Path.Combine(dataRoot.Trim(), "CheckoutRecovery");
        _path = Path.Combine(directory, FileName);
        _legacyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InventoryPOS",
            "CheckoutRecovery",
            FileName);
    }

    public string JournalFilePath => _path;

    public async Task<CreateSaleRequest?> LoadAsync(CancellationToken cancellationToken = default)
    {
        MigrateLegacyJournalIfNeeded();

        if (!File.Exists(_path))
            return null;

        await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, FileOptions.Asynchronous);
        var request = await JsonSerializer.DeserializeAsync<CreateSaleRequest>(
            stream, JsonOptions, cancellationToken);
        if (request is null || request.CheckoutRequestId == Guid.Empty ||
            request.LocationId == Guid.Empty || request.Lines is null || request.Lines.Count == 0)
            throw new InvalidDataException(
                "Pending checkout journal is invalid. Do not start another sale until it is reviewed. " +
                "Journal: " + _path);
        return request;
    }

    public async Task SaveAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CheckoutRequestId == Guid.Empty)
            throw new ArgumentException("CheckoutRequestId is required.", nameof(request));

        MigrateLegacyJournalIfNeeded();
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        // Never replace an unresolved checkout with a different one.
        if (File.Exists(_path))
            throw new InvalidOperationException(
                "An unresolved checkout journal already exists. Reconcile it before starting another sale. " +
                "Journal: " + _path);

        var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, request, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, _path, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public async Task ClearAsync(Guid confirmedCheckoutRequestId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MigrateLegacyJournalIfNeeded();

        // Fail closed if the file is missing or has been replaced unexpectedly.
        if (!File.Exists(_path))
            throw new IOException(
                "The pending checkout journal is missing; recovery needs review. Expected: " + _path);

        var saved = await LoadAsync(cancellationToken);
        if (saved?.CheckoutRequestId != confirmedCheckoutRequestId)
            throw new InvalidOperationException(
                "Checkout journal ID mismatch; refusing to clear it. Journal: " + _path);

        File.Delete(_path);
    }

    private void MigrateLegacyJournalIfNeeded()
    {
        if (File.Exists(_path) || string.IsNullOrWhiteSpace(_legacyPath) || !File.Exists(_legacyPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.Move(_legacyPath, _path, overwrite: false);

        try
        {
            var legacyDirectory = Path.GetDirectoryName(_legacyPath);
            if (legacyDirectory is not null &&
                Directory.Exists(legacyDirectory) &&
                Directory.GetFileSystemEntries(legacyDirectory).Length == 0)
            {
                Directory.Delete(legacyDirectory);
            }
        }
        catch
        {
            // Best-effort cleanup of the empty legacy folder.
        }
    }

    private static string ResolveDefaultDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "Data");
}
