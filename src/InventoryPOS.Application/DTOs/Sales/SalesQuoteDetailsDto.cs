using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class SalesQuoteDetailsDto
{
    public Guid Id { get; set; }

    public string QuoteNumber { get; set; }
        = string.Empty;

    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    public SalesQuoteStatus Status { get; set; }

    public Guid LocationId { get; set; }

    public string LocationName { get; set; }
        = string.Empty;

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; }
        = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public decimal Subtotal { get; set; }

    public decimal LineDiscountAmount { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    public int OrderDiscountType { get; set; } = 1;

    public decimal OrderDiscountValue { get; set; }

    public decimal SalesTaxRate { get; set; }

    public decimal TransportAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public Guid? SaleId { get; set; }

    public string? InvoiceNumber { get; set; }

    public int PricingVersion { get; set; } = 1;

    public List<SalesQuoteDetailsLineDto> Lines { get; set; }
        = new();
}

public class SalesQuoteDetailsLineDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; }
        = string.Empty;

    public string SKU { get; set; }
        = string.Empty;

    public string UnitSymbol { get; set; }
        = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int DiscountType { get; set; } = 1;

    public decimal DiscountValue { get; set; }

    public decimal TaxRate { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public int? WarrantyMonths { get; set; }

    public string? Description { get; set; }
}