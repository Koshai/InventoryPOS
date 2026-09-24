namespace InventoryPOS.Application.DTOs.Purchasing;

public class CreatePurchaseOrderLineRequest
{
    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public int? WarrantyMonths { get; set; }
}