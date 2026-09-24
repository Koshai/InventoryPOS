namespace InventoryPOS.Application.DTOs.Purchasing;

public class ReceivePurchaseOrderLineDto
{
    public Guid PurchaseOrderLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal OrderedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal RemainingQuantity =>
        OrderedQuantity - ReceivedQuantity;

    public decimal UnitCost { get; set; }

    public int? WarrantyMonths { get; set; }
}