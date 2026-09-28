namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseRequisitionToPurchaseOrderDto
{
    public Guid PurchaseRequisitionId { get; set; }

    public Guid SupplierId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string? Notes { get; set; }

    public List<PurchaseRequisitionToPurchaseOrderLineDto>
        Lines
    { get; set; } = [];
}

public class PurchaseRequisitionToPurchaseOrderLineDto
{
    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? EstimatedUnitCost { get; set; }
}