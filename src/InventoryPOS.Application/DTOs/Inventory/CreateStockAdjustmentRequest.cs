namespace InventoryPOS.Application.DTOs.Inventory;

public class CreateStockAdjustmentRequest
{
    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public decimal CountedQuantity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }
}