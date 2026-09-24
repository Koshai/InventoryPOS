namespace InventoryPOS.Application.DTOs.Sales;

public class SaleDetailsLineDto
{
    public Guid SaleLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public string FormattedQuantity =>
        $"{Quantity:N3} {UnitSymbol}";

    public string FormattedUnitPrice =>
        $"৳{UnitPrice:N2}";

    public string FormattedDiscount =>
        $"৳{DiscountAmount:N2}";

    public string FormattedTax =>
        $"৳{TaxAmount:N2}";

    public string FormattedLineTotal =>
        $"৳{LineTotal:N2}";
}