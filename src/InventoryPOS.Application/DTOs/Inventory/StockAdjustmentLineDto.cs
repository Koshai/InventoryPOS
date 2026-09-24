namespace InventoryPOS.Application.DTOs.Inventory;

public class StockAdjustmentLineDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal SystemQuantity { get; set; }

    public decimal CountedQuantity { get; set; }

    public decimal Difference { get; set; }

    public string SystemQuantityDisplay =>
        FormatQuantity(SystemQuantity);

    public string CountedQuantityDisplay =>
        FormatQuantity(CountedQuantity);

    public string DifferenceDisplay =>
        Difference > 0
            ? $"+{FormatQuantity(Difference)}"
            : FormatQuantity(Difference);

    private static string FormatQuantity(decimal quantity)
    {
        return quantity % 1 == 0
            ? quantity.ToString("N0")
            : quantity.ToString("N3")
                .TrimEnd('0')
                .TrimEnd('.');
    }
}