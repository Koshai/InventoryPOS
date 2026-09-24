namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal OrderedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal UnitCost { get; set; }

    public int? WarrantyMonths { get; set; }

    public decimal LineTotal =>
        OrderedQuantity * UnitCost;

    public decimal RemainingQuantity =>
        OrderedQuantity - ReceivedQuantity;

    public string FormattedLineTotal =>
        $"৳{LineTotal:N2}";

    public string WarrantyDisplay =>
        WarrantyMonths.HasValue
            ? $"{WarrantyMonths.Value} months"
            : "—";
}