using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class CreateSalesReturnRequest
{
    public Guid SaleId { get; set; }

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public List<CreateSalesReturnLineRequest> Lines { get; set; }
        = new();

    /*
     * Optional actual money refund.
     *
     * Zero means:
     * - the return may only reduce customer debt, OR
     * - refundable money exists but has not yet been paid out.
     */
    public decimal RefundAmount { get; set; }

    public PaymentMethod? RefundPaymentMethod { get; set; }

    public string? RefundReferenceNumber { get; set; }

    public string? RefundNotes { get; set; }
}

public sealed class CreateSalesReturnLineRequest
{
    public Guid SaleLineId { get; set; }

    public decimal Quantity { get; set; }

    public bool IsRestockable { get; set; } = true;

    public string? Reason { get; set; }
}