
using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class WarrantyReplacement : BaseEntity
{
    public Guid WarrantyClaimItemId { get; set; }

    public Guid ReplacementVariantId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? InventoryBatchId { get; set; }

    public Guid StockMovementId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime IssuedAtUtc { get; set; }

    public string? Notes { get; set; }

    public WarrantyClaimItem? WarrantyClaimItem { get; set; }

    public ProductVariant? ReplacementVariant { get; set; }

    public Location? Location { get; set; }

    public InventoryBatch? InventoryBatch { get; set; }

    public StockMovement? StockMovement { get; set; }
}
