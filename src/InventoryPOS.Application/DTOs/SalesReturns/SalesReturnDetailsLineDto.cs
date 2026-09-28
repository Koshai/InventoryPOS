namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnDetailsLineDto
{
    public Guid SalesReturnLineId { get; set; }

    public Guid SaleLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } =
        string.Empty;

    public string SKU { get; set; } =
        string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsRestockable { get; set; }

    public string? Reason { get; set; }
    public string RestockText =>
    IsRestockable
        ? "Yes"
        : "No";

    public string QuantityText =>
        Quantity.ToString("N3");

    public string UnitPriceText =>
        $"৳{UnitPrice:N2}";

    public string LineTotalText =>
        $"৳{LineTotal:N2}";
}