using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.Services;

/// <summary>
/// Stores one unresolved customer payment per workstation.
/// Stored under Storage:DataRoot (not MSIX LocalCache) so operators can find it.
/// PostgreSQL remains the authority on whether payment committed.
/// </summary>
public sealed class PendingCustomerPaymentJournalService
    : IPendingCustomerPaymentJournalService
{
    private const string FileName = "pending-customer-payment.json";

    private readonly string _path;
    private readonly string? _legacyPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public PendingCustomerPaymentJournalService()
        : this(ResolveDefaultDataRoot())
    {
    }

    public PendingCustomerPaymentJournalService(string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("Data root is required.", nameof(dataRoot));

        var directory = Path.Combine(dataRoot.Trim(), "CustomerPaymentRecovery");
        _path = Path.Combine(directory, FileName);
        _legacyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InventoryPOS",
            "CustomerPaymentRecovery",
            FileName);
    }

    public string JournalFilePath => _path;

    public async Task<PendingCustomerPayment?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        MigrateLegacyJournalIfNeeded();

        if (!File.Exists(_path))
            return null;

        await using var stream = new FileStream(
            _path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous);

        var payment =
            await JsonSerializer.DeserializeAsync<PendingCustomerPayment>(
                stream,
                JsonOptions,
                cancellationToken);

        Validate(payment);

        return payment;
    }

    public async Task SaveAsync(
        PendingCustomerPayment payment,
        CancellationToken cancellationToken = default)
    {
        Validate(payment);
        MigrateLegacyJournalIfNeeded();

        Directory.CreateDirectory(
            Path.GetDirectoryName(_path)!);

        if (File.Exists(_path))
        {
            throw new InvalidOperationException(
                "An unresolved customer payment already exists. " +
                "Reconcile it before recording another payment. " +
                "Journal: " + _path);
        }

        var temporaryPath =
            _path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    payment,
                    JsonOptions,
                    cancellationToken);

                await stream.FlushAsync(cancellationToken);

                stream.Flush(flushToDisk: true);
            }

            File.Move(
                temporaryPath,
                _path,
                overwrite: false);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public async Task ClearAsync(
        Guid confirmedPaymentRequestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MigrateLegacyJournalIfNeeded();

        if (!File.Exists(_path))
        {
            throw new IOException(
                "The pending customer payment journal is missing. " +
                "Recovery requires review. Expected: " + _path);
        }

        var saved = await LoadAsync(cancellationToken);

        if (saved?.Request.PaymentRequestId !=
            confirmedPaymentRequestId)
        {
            throw new InvalidOperationException(
                "Customer payment journal ID mismatch. " +
                "Refusing to clear it. Journal: " + _path);
        }

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

    private static void Validate(
        PendingCustomerPayment? payment)
    {
        if (payment?.Request is null)
        {
            throw new InvalidDataException(
                "Pending customer payment journal is invalid. Journal may be at a DataRoot path.");
        }

        var request = payment.Request;

        if (request.PaymentRequestId == Guid.Empty ||
            request.CustomerId == Guid.Empty ||
            request.Amount <= 0m ||
            request.PaymentDate == default)
        {
            throw new InvalidDataException(
                "Pending customer payment contains invalid details.");
        }

        if (payment.IsInvoicePayment)
        {
            if (!payment.TargetSaleId.HasValue ||
                payment.TargetSaleId.Value == Guid.Empty)
            {
                throw new InvalidDataException(
                    "Pending invoice payment has no target invoice.");
            }
        }
        else if (payment.TargetSaleId.HasValue)
        {
            throw new InvalidDataException(
                "General account payment must not specify an invoice.");
        }
    }

    private static string ResolveDefaultDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "Data");
}
