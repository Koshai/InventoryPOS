using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class SaleDetailsPaymentDto
{
    public Guid SalePaymentId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string PaymentMethodDisplay =>
        PaymentMethod switch
        {
            PaymentMethod.Bkash => "bKash",
            PaymentMethod.Nagad => "Nagad",
            PaymentMethod.Rocket => "Rocket",
            PaymentMethod.BankTransfer => "Bank Transfer",
            _ => PaymentMethod.ToString()
        };

    public string FormattedAmount =>
        $"৳{Amount:N2}";

    public string FormattedPaymentDate =>
        PaymentDate.ToString("dd MMM yyyy, hh:mm tt");

    public string ReferenceDisplay =>
        string.IsNullOrWhiteSpace(ReferenceNumber)
            ? "—"
            : ReferenceNumber;
}