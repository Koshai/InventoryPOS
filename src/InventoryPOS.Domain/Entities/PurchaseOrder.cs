using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public Guid LocationId { get; set; }

    public PurchaseOrderStatus Status { get; set; }
        = PurchaseOrderStatus.Draft;

    public DateTime OrderDate { get; set; }
        = DateTime.UtcNow;

    public DateTime? ExpectedDate { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Supplier? Supplier { get; set; }

    public Location? Location { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; }
        = new List<PurchaseOrderLine>();
}