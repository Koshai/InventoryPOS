namespace InventoryPOS.Application.DTOs.Purchasing;

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyList<CreatePurchaseOrderLineRequest> Lines { get; set; }
        = Array.Empty<CreatePurchaseOrderLineRequest>();
}