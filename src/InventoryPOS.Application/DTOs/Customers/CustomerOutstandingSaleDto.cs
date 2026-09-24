using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Customers;

public sealed class CustomerOutstandingSaleDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public SaleStatus Status { get; set; }
    public string FormattedSaleDate =>
    SaleDate.ToLocalTime().ToString("dd MMM yyyy");

    public string FormattedTotal =>
        $"৳{TotalAmount:N2}";

    public string FormattedPaid =>
        $"৳{PaidAmount:N2}";

    public string FormattedDue =>
        $"৳{DueAmount:N2}";

    public string StatusDisplay =>
        Status.ToString();
}