namespace InventoryPOS.Application.DTOs.Sales;

public class CreateSalesQuoteRequest
{
    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    // Explicitly select the new pricing calculation path.
    // Existing callers remain compatible by default.
    public bool UsePricingCalculator { get; set; }

    public int OrderDiscountType { get; set; } = 1;

    public decimal OrderDiscountValue { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    // Existing aggregate discount snapshot.
    public decimal DiscountAmount { get; set; }

    public decimal SalesTaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TransportAmount { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public IReadOnlyList<CreateSalesQuoteLineRequest> Lines
    { get; set; }
        = Array.Empty<CreateSalesQuoteLineRequest>();
}

public class CreateSalesQuoteLineRequest
{
    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int DiscountType { get; set; } = 1;

    public decimal DiscountValue { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public int? WarrantyMonths { get; set; }

    public string? Description { get; set; }
}