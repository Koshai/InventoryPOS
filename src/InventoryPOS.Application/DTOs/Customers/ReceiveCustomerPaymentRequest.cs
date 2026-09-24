using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Customers;

public sealed class ReceiveCustomerPaymentRequest
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public DateTime PaymentDate { get; set; } =
        DateTime.UtcNow;

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}