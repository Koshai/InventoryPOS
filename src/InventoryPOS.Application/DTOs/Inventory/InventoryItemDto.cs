namespace InventoryPOS.Application.DTOs.Inventory;

public class InventoryItemDto
{
    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal QuantityAvailable =>
        QuantityOnHand - QuantityReserved;

    public decimal SellingPrice { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsLowStock =>
        QuantityOnHand <= ReorderLevel;
}