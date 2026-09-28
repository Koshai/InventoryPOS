using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class GoodsReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }

    public Guid SupplierId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime ReceivedAt { get; set; }
        = DateTime.UtcNow;

    public string? SupplierInvoiceNumber { get; set; }

    public string? Notes { get; set; }

    public Guid? ReceivedByUserId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public Supplier? Supplier { get; set; }

    public Location? Location { get; set; }

    public ICollection<GoodsReceiptLine> Lines { get; set; }
        = new List<GoodsReceiptLine>();
    public ICollection<PurchaseReturn> Returns { get; set; }
    = new List<PurchaseReturn>();
}