using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class StockAdjustmentLine : BaseEntity
{
    public Guid StockAdjustmentId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal CountedQuantity { get; set; }

    public decimal Difference { get; set; }

    public StockAdjustment? StockAdjustment { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}