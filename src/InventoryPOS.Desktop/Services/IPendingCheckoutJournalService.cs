using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Desktop.Services;

public interface IPendingCheckoutJournalService
{
    Task<CreateSaleRequest?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task ClearAsync(Guid confirmedCheckoutRequestId, CancellationToken cancellationToken = default);
}
