using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class SalesHistoryDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public string CustomerName { get; set; } = "Walk-in Customer";

    public string LocationName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public SaleStatus Status { get; set; }

    public string FormattedDate =>
        SaleDate.ToString("dd MMM yyyy");

    public string FormattedTime =>
        SaleDate.ToString("hh:mm tt");

    public string FormattedTotal =>
        $"৳{TotalAmount:N2}";

    public string FormattedPaid =>
        $"৳{PaidAmount:N2}";

    public string FormattedDue =>
        $"৳{DueAmount:N2}";

    public string StatusDisplay =>
        Status switch
        {
            SaleStatus.Completed => "Fully Paid",
            SaleStatus.PartiallyPaid => "Partially Paid",
            SaleStatus.Credit => "Credit",
            SaleStatus.Cancelled => "Cancelled",
            SaleStatus.Refunded => "Refunded",
            SaleStatus.Draft => "Draft",
            _ => Status.ToString()
        };
}