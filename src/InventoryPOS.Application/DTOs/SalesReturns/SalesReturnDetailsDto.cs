using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnDetailsDto
{
    public Guid SalesReturnId { get; set; }

    public string ReturnNumber { get; set; } =
        string.Empty;

    public DateTime ReturnDate { get; set; }

    public SalesReturnStatus Status { get; set; }

    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } =
        string.Empty;

    public DateTime SaleDate { get; set; }

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } =
        string.Empty;

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; } =
        string.Empty;

    public decimal OriginalSaleTotal { get; set; }

    public decimal ReturnAmount { get; set; }

    public decimal CreditAdjustmentAmount { get; set; }

    public decimal RefundedAmount { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public List<SalesReturnDetailsLineDto> Lines { get; set; } =
        new();

    public List<SalesReturnRefundDto> Refunds { get; set; } =
        new();
}