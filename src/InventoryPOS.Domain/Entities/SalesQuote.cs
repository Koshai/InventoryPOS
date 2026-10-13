using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class SalesQuote : BaseEntity
{
    public string QuoteNumber { get; set; }
        = string.Empty;

    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    public SalesQuoteStatus Status { get; set; }
        = SalesQuoteStatus.Draft;

    // Existing gross subtotal, before discounts.
    public decimal Subtotal { get; set; }

    // New pricing snapshots.
    public decimal LineDiscountAmount { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    // Total line + order discount.
    public decimal DiscountAmount { get; set; }

    public int OrderDiscountType { get; set; } = 1;

    public decimal OrderDiscountValue { get; set; }

    public decimal SalesTaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TransportAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public Guid? SaleId { get; set; }

    public Location? Location { get; set; }

    public Customer? Customer { get; set; }

    public Sale? Sale { get; set; }

    // Version 1: Legacy quotation pricing.
    // Version 2: Shared pricing calculator with
    // line discounts, order discounts, tax and transport.
    public int PricingVersion { get; set; } = 1;

    public ICollection<SalesQuoteLine> Lines { get; set; }
        = new List<SalesQuoteLine>();
}