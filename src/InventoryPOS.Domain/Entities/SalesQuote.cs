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

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public Guid? SaleId { get; set; }

    public Location? Location { get; set; }

    public Customer? Customer { get; set; }

    public Sale? Sale { get; set; }

    public ICollection<SalesQuoteLine> Lines { get; set; }
        = new List<SalesQuoteLine>();
}