using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class SalePayment : BaseEntity
{
    public Guid SaleId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public Sale? Sale { get; set; }

    public Guid? PaymentRequestId { get; set; }
}