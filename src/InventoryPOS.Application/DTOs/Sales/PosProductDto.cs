namespace InventoryPOS.Application.DTOs.Sales;

public class PosProductDto
{
    public Guid ProductVariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }

    public decimal QuantityAvailable { get; set; }

    public bool IsInStock => QuantityAvailable > 0;

    public string FormattedPrice =>
        $"৳{SellingPrice:N2}";

    public string StockDisplay =>
        $"{QuantityAvailable:N3} {UnitSymbol}";
}