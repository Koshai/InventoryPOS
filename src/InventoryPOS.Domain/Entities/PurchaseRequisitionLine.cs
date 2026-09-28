using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class PurchaseRequisitionLine : BaseEntity
{
    public Guid PurchaseRequisitionId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal? EstimatedUnitCost { get; set; }

    public decimal EstimatedLineTotal { get; set; }

    public string? Description { get; set; }

    public string? Notes { get; set; }

    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}