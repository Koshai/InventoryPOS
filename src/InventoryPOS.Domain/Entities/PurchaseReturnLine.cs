using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class PurchaseReturnLine : BaseEntity
{
    public Guid PurchaseReturnId { get; set; }

    public Guid GoodsReceiptLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid? InventoryBatchId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }

    public string? Reason { get; set; }

    public PurchaseReturn? PurchaseReturn { get; set; }

    public GoodsReceiptLine? GoodsReceiptLine { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public InventoryBatch? InventoryBatch { get; set; }
}