using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class SalesRefund : BaseEntity
{
    public Guid SalesReturnId { get; set; }

    public SalesRefundType RefundType { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public DateTime RefundDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public SalesReturn? SalesReturn { get; set; }
}