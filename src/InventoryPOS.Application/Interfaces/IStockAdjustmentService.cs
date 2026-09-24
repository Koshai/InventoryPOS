using InventoryPOS.Application.DTOs.Inventory;

namespace InventoryPOS.Application.Interfaces;

public interface IStockAdjustmentService
{
    Task<decimal?> GetCurrentQuantityAsync(
        Guid productVariantId,
        Guid locationId,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentResultDto> CreateAsync(
        CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentDetailsDto?> GetByIdAsync(
        Guid adjustmentId,
        CancellationToken cancellationToken = default);
}