using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnRefundDto
{
    public Guid SalesRefundId { get; set; }

    public SalesRefundType RefundType { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public DateTime RefundDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
    public string RefundTypeText =>
    RefundType switch
    {
        SalesRefundType.PaymentRefund =>
            "Payment Refund",

        SalesRefundType.CustomerCreditAdjustment =>
            "Applied to Balance",

        SalesRefundType.NoRefund =>
            "No Refund",

        _ =>
            RefundType.ToString()
    };

    public string PaymentMethodText =>
        PaymentMethod?.ToString() ?? "—";

    public string AmountText =>
        $"৳{Amount:N2}";

    public string RefundDateText =>
        RefundDate.ToLocalTime()
            .ToString("dd MMM yyyy, hh:mm tt");

    public string ReferenceText =>
        string.IsNullOrWhiteSpace(ReferenceNumber)
            ? "—"
            : ReferenceNumber;
}