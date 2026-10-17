using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Desktop.Services;

public interface IPendingCheckoutJournalService
{
    /// <summary>
    /// Absolute path of the durable recovery journal file.
    /// </summary>
    string JournalFilePath { get; }

    Task<CreateSaleRequest?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task ClearAsync(Guid confirmedCheckoutRequestId, CancellationToken cancellationToken = default);
}
