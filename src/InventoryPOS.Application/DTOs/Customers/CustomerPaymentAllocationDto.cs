namespace InventoryPOS.Application.DTOs.Customers;

public sealed class CustomerPaymentAllocationDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } =
        string.Empty;

    public decimal AmountApplied { get; set; }

    public decimal RemainingDue { get; set; }
}