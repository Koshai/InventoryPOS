namespace InventoryPOS.Application.DTOs.Inventory;

public class StockAdjustmentResultDto
{
    public Guid AdjustmentId { get; set; }

    public decimal PreviousQuantity { get; set; }

    public decimal CountedQuantity { get; set; }

    public decimal Difference { get; set; }

    public string MovementType { get; set; } = string.Empty;
}