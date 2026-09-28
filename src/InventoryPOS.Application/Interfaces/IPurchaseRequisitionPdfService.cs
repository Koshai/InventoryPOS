using InventoryPOS.Application.DTOs.Purchasing;

namespace InventoryPOS.Application.Interfaces;

public interface IPurchaseRequisitionPdfService
{
    Task<byte[]> GeneratePdfAsync(
        PurchaseRequisitionDetailsDto requisition,
        CancellationToken cancellationToken = default);
}