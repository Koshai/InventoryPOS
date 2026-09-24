namespace InventoryPOS.Application.DTOs.Sales;

public class SalesHistoryResultDto
{
    public IReadOnlyList<SalesHistoryDto> Items { get; set; }
        = Array.Empty<SalesHistoryDto>();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);

    public bool HasPreviousPage =>
        PageNumber > 1;

    public bool HasNextPage =>
        PageNumber < TotalPages;
}