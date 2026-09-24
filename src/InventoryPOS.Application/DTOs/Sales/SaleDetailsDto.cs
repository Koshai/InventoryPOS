using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class SaleDetailsDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public SaleStatus Status { get; set; }

    // Location

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    // Customer

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; } = "Walk-in Customer";

    public string? CustomerCode { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerAddress { get; set; }

    // Totals

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public string? Notes { get; set; }

    // Children

    public IReadOnlyList<SaleDetailsLineDto> Lines { get; set; }
        = Array.Empty<SaleDetailsLineDto>();

    public IReadOnlyList<SaleDetailsPaymentDto> Payments { get; set; }
        = Array.Empty<SaleDetailsPaymentDto>();

    // Display

    public string FormattedSaleDate =>
        SaleDate.ToString("dd MMM yyyy, hh:mm tt");

    public string FormattedSubtotal =>
        $"৳{Subtotal:N2}";

    public string FormattedDiscount =>
        $"৳{DiscountAmount:N2}";

    public string FormattedTax =>
        $"৳{TaxAmount:N2}";

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

    public bool IsWalkIn =>
        !CustomerId.HasValue;

    public bool HasPayments =>
        Payments.Count > 0;

    public bool HasDue =>
        DueAmount > 0;
}