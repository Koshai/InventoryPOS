using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class SalesReturn : BaseEntity
{
    public string ReturnNumber { get; set; }
        = string.Empty;

    public Guid? ReturnRequestId { get; set; }

    public string? ReturnRequestFingerprint { get; set; }

    public Guid SaleId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime ReturnDate { get; set; }

    public SalesReturnStatus Status { get; set; }

    public decimal Subtotal { get; set; }

    public decimal RefundAmount { get; set; }

    public decimal CreditAdjustmentAmount { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public Sale? Sale { get; set; }

    public Location? Location { get; set; }

    public Customer? Customer { get; set; }
    public string? ReturnResultJson { get; set; }

    public ICollection<SalesReturnLine> Lines { get; set; }
        = new List<SalesReturnLine>();

    public ICollection<SalesRefund> Refunds { get; set; }
        = new List<SalesRefund>();
}