using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class InventoryBatch : BaseEntity
{
    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public Guid SupplierId { get; set; }

    public Guid? GoodsReceiptLineId { get; set; }

    public string SupplierBatchNumber { get; set; }
        = string.Empty;

    public DateTime? ManufactureDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal UnitCost { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public Location? Location { get; set; }

    public Supplier? Supplier { get; set; }

    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
}