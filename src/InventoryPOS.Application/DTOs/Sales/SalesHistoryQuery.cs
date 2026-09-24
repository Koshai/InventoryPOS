using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class SalesHistoryQuery
{
    public string? SearchText { get; set; }

    public SaleStatus? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Guid? LocationId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}