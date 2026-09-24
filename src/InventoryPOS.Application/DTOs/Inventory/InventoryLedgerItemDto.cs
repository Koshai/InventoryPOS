namespace InventoryPOS.Application.DTOs.Inventory;

public class InventoryLedgerItemDto
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public string MovementType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? Notes { get; set; }

    public string FormattedQuantity
    {
        get
        {
            var absolute = Math.Abs(Quantity);

            var formatted =
                absolute % 1 == 0
                    ? absolute.ToString("N0")
                    : absolute.ToString("N3")
                        .TrimEnd('0')
                        .TrimEnd('.');

            if (Quantity > 0)
                return $"+{formatted}";

            if (Quantity < 0)
                return $"-{formatted}";

            return formatted;
        }
    }

    public string FormattedDate =>
        CreatedAt.ToLocalTime()
            .ToString("MMM dd, yyyy h:mm tt");

    public string ReferenceDisplay =>
        string.IsNullOrWhiteSpace(ReferenceType)
            ? "—"
            : ReferenceType;

    public bool CanOpenReference =>
        ReferenceType == "StockAdjustment" &&
        ReferenceId.HasValue;
}