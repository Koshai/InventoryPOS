using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Purchasing;

namespace InventoryPOS.Application.Interfaces;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderDetailsDto?> GetByIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierLookupDto>> GetSuppliersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryProductLookupDto>> GetProductVariantsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationLookupDto>> GetLocationsAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default);
}