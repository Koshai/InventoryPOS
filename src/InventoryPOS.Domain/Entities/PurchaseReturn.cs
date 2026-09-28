using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class PurchaseReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;

    public Guid GoodsReceiptId { get; set; }

    public Guid SupplierId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime ReturnDate { get; set; }

    public PurchaseReturnStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }

    public Supplier? Supplier { get; set; }

    public Location? Location { get; set; }

    public ICollection<PurchaseReturnLine> Lines { get; set; }
        = new List<PurchaseReturnLine>();
}