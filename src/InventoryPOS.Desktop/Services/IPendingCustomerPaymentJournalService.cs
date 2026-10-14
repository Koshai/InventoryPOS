using System;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.Services;

public interface IPendingCustomerPaymentJournalService
{
    Task<PendingCustomerPayment?> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        PendingCustomerPayment payment,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        Guid confirmedPaymentRequestId,
        CancellationToken cancellationToken = default);
}