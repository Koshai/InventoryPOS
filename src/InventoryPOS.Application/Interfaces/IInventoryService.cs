using InventoryPOS.Application.DTOs.Inventory;

namespace InventoryPOS.Application.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetInventoryAsync(
        CancellationToken cancellationToken = default);

    Task SetOpeningStockAsync(
        SetOpeningStockRequest request,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<InventoryProductLookupDto>>
    GetProductVariantsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationLookupDto>>
        GetLocationsAsync(
            CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLedgerItemDto>>
        GetInventoryLedgerAsync(
            CancellationToken cancellationToken = default);
}