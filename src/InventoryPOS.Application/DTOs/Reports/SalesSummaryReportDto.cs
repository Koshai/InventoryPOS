namespace InventoryPOS.Application.DTOs.Reports;

/// <summary>
/// Canonical sales totals for a period. Definitions:
/// GrossSales = sum of Subtotal for non-cancelled sales (before order-level discounts/tax).
/// DiscountAmount = sum of DiscountAmount on those sales.
/// TaxAmount = sum of TaxAmount.
/// NetSales = sum of TotalAmount minus recognized sales-return totals in the period.
/// PaymentsCollected = sum of SalePayment amounts with PaymentDate in range.
/// </summary>
public sealed class SalesSummaryReportDto
{
    public DateTimeOffset FromUtc { get; set; }

    public DateTimeOffset ToUtc { get; set; }

    public Guid? LocationId { get; set; }

    public int TransactionCount { get; set; }

    public decimal GrossSales { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TransportAmount { get; set; }

    public decimal InvoiceTotal { get; set; }

    public decimal SalesReturnsAmount { get; set; }

    public decimal NetSales { get; set; }

    public decimal PaymentsCollected { get; set; }

    public decimal AverageTransactionValue { get; set; }
}
