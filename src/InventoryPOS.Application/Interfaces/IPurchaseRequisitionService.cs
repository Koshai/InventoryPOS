using InventoryPOS.Application.DTOs.Purchasing;

namespace InventoryPOS.Application.Interfaces;

public interface IPurchaseRequisitionService
{
    Task<PurchaseRequisitionResultDto> CreateAsync(
        CreatePurchaseRequisitionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseRequisitionHistoryDto>> GetHistoryAsync(
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequisitionDetailsDto?> GetByIdAsync(
        Guid purchaseRequisitionId,
        CancellationToken cancellationToken = default);

    Task MarkAsSentAsync(
    Guid purchaseRequisitionId,
    CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid purchaseRequisitionId,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequisitionToPurchaseOrderDto>
    GetPurchaseOrderConversionAsync(
        Guid purchaseRequisitionId,
        CancellationToken cancellationToken = default);

    Task CompletePurchaseOrderConversionAsync(
        Guid purchaseRequisitionId,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);
}