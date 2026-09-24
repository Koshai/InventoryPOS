using InventoryPOS.Application.DTOs.Purchasing;

namespace InventoryPOS.Application.Interfaces;

public interface IGoodsReceiptService
{
    Task<ReceivePurchaseOrderDto?> GetPurchaseOrderForReceiptAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateReceiptAsync(
        CreateGoodsReceiptRequest request,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<GoodsReceiptListItemDto>>
    GetByPurchaseOrderAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<GoodsReceiptDetailsDto?> GetByIdAsync(
        Guid goodsReceiptId,
        CancellationToken cancellationToken = default);
}