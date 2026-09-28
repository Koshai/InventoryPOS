namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnHistoryResultDto
{
    public List<SalesReturnHistoryItemDto> Items { get; set; } =
        new();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);
}