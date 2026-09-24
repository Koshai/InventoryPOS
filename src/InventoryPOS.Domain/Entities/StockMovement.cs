using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public InventoryMovementType MovementType { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid? UserId { get; set; }

    public string? Notes { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public Location? Location { get; set; }
    public Guid? InventoryBatchId { get; set; }

    public InventoryBatch? InventoryBatch { get; set; }
}