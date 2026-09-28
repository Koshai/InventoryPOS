using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class PurchaseRequisition : BaseEntity
{
    public string RequisitionNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime RequisitionDate { get; set; } = DateTime.UtcNow;

    public DateTime? RequiredByDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    public PurchaseRequisitionStatus Status { get; set; }
        = PurchaseRequisitionStatus.Draft;

    public decimal EstimatedTotal { get; set; }

    public string? Subject { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public Guid? CreatedByUserId { get; set; }

    // Set when this requisition becomes a PO.
    public Guid? PurchaseOrderId { get; set; }

    public Supplier? Supplier { get; set; }

    public Location? Location { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public ICollection<PurchaseRequisitionLine> Lines { get; set; }
        = new List<PurchaseRequisitionLine>();
}