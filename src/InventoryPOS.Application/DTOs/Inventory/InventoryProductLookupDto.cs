namespace InventoryPOS.Application.DTOs.Inventory;

public class InventoryProductLookupDto
{
    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public string DisplayName =>
        $"{ProductName} ({SKU})";
}