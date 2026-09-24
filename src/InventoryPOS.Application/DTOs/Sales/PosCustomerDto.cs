namespace InventoryPOS.Application.DTOs.Sales;

public class PosCustomerDto
{
    public Guid Id { get; set; }

    public string CustomerCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }

    public string DisplayName =>
        $"{CustomerCode} - {Name}";

    public decimal OutstandingBalance { get; set; }

    public decimal AvailableCredit =>
        Math.Max(0, CreditLimit - OutstandingBalance);

    public string FormattedCreditLimit =>
        $"৳{CreditLimit:N2}";

    public string FormattedOutstandingBalance =>
        $"৳{OutstandingBalance:N2}";

    public string FormattedAvailableCredit =>
        $"৳{AvailableCredit:N2}";
}