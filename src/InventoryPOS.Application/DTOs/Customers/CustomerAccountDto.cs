namespace InventoryPOS.Application.DTOs.Customers;

public sealed class CustomerAccountDto
{
    public Guid CustomerId { get; set; }

    public string CustomerCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal OutstandingBalance { get; set; }

    public decimal AvailableCredit =>
        Math.Max(0, CreditLimit - OutstandingBalance);

    public List<CustomerOutstandingSaleDto> OutstandingSales
    { get; set; } = new();

    public List<CustomerPaymentHistoryDto> PaymentHistory
    { get; set; } = new();
}