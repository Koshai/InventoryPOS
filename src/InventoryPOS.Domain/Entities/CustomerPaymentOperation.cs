using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public sealed class CustomerPaymentOperation : BaseEntity
{
    public Guid PaymentRequestId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? TargetSaleId { get; set; }

    public decimal Amount { get; set; }

    public string RequestFingerprint { get; set; } =
        string.Empty;

    public string ResultJson { get; set; } =
        string.Empty;
}