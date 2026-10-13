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

    public decimal Subtotal { get; set; }

    public decimal LineDiscountAmount { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal SalesTaxRate { get; set; }

    public decimal TransportAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public SaleStatus Status { get; set; }

    public bool IsPricingExpanded { get; set; }

    public bool HasLineDiscount => LineDiscountAmount != 0m;

    public bool HasOrderDiscount => OrderDiscountAmount != 0m;

    public bool HasTax => TaxAmount != 0m;

    public bool HasTransport => TransportAmount != 0m;

    public bool HasOtherDiscount =>
        OtherDiscountAmount != 0m;

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

    public decimal OtherDiscountAmount =>
    DiscountAmount - LineDiscountAmount - OrderDiscountAmount;

    public string FormattedSubtotal => $"৳{Subtotal:N2}";

    public string FormattedLineDiscount =>
        $"-৳{LineDiscountAmount:N2}";

    public string FormattedOrderDiscount =>
        $"-৳{OrderDiscountAmount:N2}";

    public string FormattedOtherDiscount =>
        $"-৳{OtherDiscountAmount:N2}";

    public string FormattedTax =>
        $"+৳{TaxAmount:N2}";

    public string FormattedTransport =>
        $"+৳{TransportAmount:N2}";
}