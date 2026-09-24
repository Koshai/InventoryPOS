using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class CreateSalePaymentRequest
{
    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public string? ReferenceNumber { get; set; }
}