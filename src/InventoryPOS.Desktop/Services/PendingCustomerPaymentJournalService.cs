using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.Services;

/// <summary>
/// Stores one unresolved customer payment per Windows user profile.
/// PostgreSQL remains the authority on whether payment committed.
/// </summary>
public sealed class PendingCustomerPaymentJournalService
    : IPendingCustomerPaymentJournalService
{
    private readonly string _path;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public PendingCustomerPaymentJournalService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "InventoryPOS",
            "CustomerPaymentRecovery");

        _path = Path.Combine(
            directory,
            "pending-customer-payment.json");
    }

    public async Task<PendingCustomerPayment?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
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

        Directory.CreateDirectory(
            Path.GetDirectoryName(_path)!);

        if (File.Exists(_path))
        {
            throw new InvalidOperationException(
                "An unresolved customer payment already exists. " +
                "Reconcile it before recording another payment.");
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

        if (!File.Exists(_path))
        {
            throw new IOException(
                "The pending customer payment journal is missing. " +
                "Recovery requires review.");
        }

        var saved = await LoadAsync(cancellationToken);

        if (saved?.Request.PaymentRequestId !=
            confirmedPaymentRequestId)
        {
            throw new InvalidOperationException(
                "Customer payment journal ID mismatch. " +
                "Refusing to clear it.");
        }

        File.Delete(_path);
    }

    private static void Validate(
        PendingCustomerPayment? payment)
    {
        if (payment?.Request is null)
        {
            throw new InvalidDataException(
                "Pending customer payment journal is invalid.");
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
}