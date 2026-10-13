using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class SalesQuoteLine : BaseEntity
{
    public Guid SalesQuoteId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int DiscountType { get; set; } = 1;

    public decimal DiscountValue { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public int? WarrantyMonths { get; set; }

    public string? Description { get; set; }

    public SalesQuote? SalesQuote { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}