using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal UnitCost { get; set; }
    public int? WarrantyMonths { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public ProductVariant? ProductVariant { get; set; }
    
    public decimal LineTotal =>
        OrderedQuantity * UnitCost;

    public decimal RemainingQuantity =>
        OrderedQuantity - ReceivedQuantity;
}