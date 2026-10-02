
namespace InventoryPOS.Application.DTOs.Warranty;

public sealed class WarrantyInvoiceSearchPageDto
{
    public List<WarrantyInvoiceSearchDto> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
