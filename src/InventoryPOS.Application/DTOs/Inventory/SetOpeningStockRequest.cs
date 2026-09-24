namespace InventoryPOS.Application.DTOs.Inventory;

public class SetOpeningStockRequest
{
    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Notes { get; set; }
}