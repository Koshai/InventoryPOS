using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Customers;

public sealed class CustomerPaymentHistoryDto
{
    public Guid SalePaymentId { get; set; }

    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
    public string FormattedPaymentDate =>
    PaymentDate.ToLocalTime()
        .ToString("dd MMM yyyy, hh:mm tt");

    public string PaymentMethodDisplay =>
        PaymentMethod switch
        {
            PaymentMethod.Bkash => "bKash",
            PaymentMethod.BankTransfer => "Bank Transfer",
            _ => PaymentMethod.ToString()
        };

    public string FormattedAmount =>
        $"৳{Amount:N2}";

    public string ReferenceDisplay =>
        string.IsNullOrWhiteSpace(ReferenceNumber)
            ? "—"
            : ReferenceNumber;
}