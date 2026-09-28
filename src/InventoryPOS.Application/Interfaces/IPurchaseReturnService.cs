using InventoryPOS.Application.DTOs.Purchasing;

namespace InventoryPOS.Application.Interfaces;

public interface IPurchaseReturnService
{
    Task<PurchaseReturnableReceiptDto?>
        GetReturnableReceiptAsync(
            Guid goodsReceiptId,
            CancellationToken cancellationToken = default);

    Task<PurchaseReturnResultDto>
        CreateReturnAsync(
            CreatePurchaseReturnRequest request,
            CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<PurchaseReturnHistoryDto>>
        GetHistoryAsync(
            string? search = null,
            CancellationToken cancellationToken = default);

    Task<PurchaseReturnDetailsDto?>
        GetByIdAsync(
            Guid purchaseReturnId,
            CancellationToken cancellationToken = default);
}