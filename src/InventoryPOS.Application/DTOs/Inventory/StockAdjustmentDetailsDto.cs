namespace InventoryPOS.Application.DTOs.Inventory;

public class StockAdjustmentDetailsDto
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public IReadOnlyList<StockAdjustmentLineDto> Lines { get; set; }
        = Array.Empty<StockAdjustmentLineDto>();

    public string FormattedDate =>
        (CompletedAt ?? CreatedAt)
            .ToLocalTime()
            .ToString("MMM dd, yyyy h:mm tt");
}