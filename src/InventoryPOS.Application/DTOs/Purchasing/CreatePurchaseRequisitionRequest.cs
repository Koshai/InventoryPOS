namespace InventoryPOS.Application.DTOs.Purchasing;

public class CreatePurchaseRequisitionRequest
{
    public Guid SupplierId { get; set; }
    public Guid LocationId { get; set; }

    public DateTime RequisitionDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredByDate { get; set; }
    public DateTime? ValidUntilDate { get; set; }

    public string? Subject { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }

    public List<CreatePurchaseRequisitionLineRequest> Lines { get; set; } = [];
}

public class CreatePurchaseRequisitionLineRequest
{
    public Guid ProductVariantId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal? EstimatedUnitCost { get; set; }

    public string? Description { get; set; }
    public string? Notes { get; set; }
}