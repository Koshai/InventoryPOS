using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnHistoryQueryDto
{
    public string? SearchText { get; set; }

    public SalesReturnStatus? Status { get; set; }

    public Guid? LocationId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 25;
}