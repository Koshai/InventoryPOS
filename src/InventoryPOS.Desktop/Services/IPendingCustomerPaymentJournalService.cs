using System;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.Services;

public interface IPendingCustomerPaymentJournalService
{
    /// <summary>
    /// Absolute path of the durable recovery journal file.
    /// </summary>
    string JournalFilePath { get; }

    Task<PendingCustomerPayment?> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        PendingCustomerPayment payment,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        Guid confirmedPaymentRequestId,
        CancellationToken cancellationToken = default);
}
