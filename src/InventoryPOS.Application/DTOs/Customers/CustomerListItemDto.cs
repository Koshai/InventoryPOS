namespace InventoryPOS.Application.DTOs.Customers;

public class CustomerListItemDto
{
    public Guid Id { get; set; }

    public string CustomerCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; }

    public string PhoneDisplay =>
        string.IsNullOrWhiteSpace(Phone) ? "—" : Phone;

    public string EmailDisplay =>
        string.IsNullOrWhiteSpace(Email) ? "—" : Email;

    public string FormattedCreditLimit =>
        $"৳{CreditLimit:N2}";

    public string StatusDisplay =>
        IsActive ? "Active" : "Inactive";
}