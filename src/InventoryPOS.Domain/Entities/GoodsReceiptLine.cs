using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class GoodsReceiptLine : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }

    public Guid PurchaseOrderLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal UnitCost { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }

    public PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public ProductVariant? ProductVariant { get; set; }
    public string? SupplierBatchNumber { get; set; }

    public DateTime? ManufactureDate { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public int? WarrantyMonths { get; set; }

    public decimal LineTotal =>
        ReceivedQuantity * UnitCost;
}