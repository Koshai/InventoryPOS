namespace InventoryPOS.Application.DTOs.Sales;

public class CreateSaleRequest
{
    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public string? Notes { get; set; }

    public Guid? SalesQuoteId { get; set; }

    public IReadOnlyList<CreateSaleLineRequest> Lines { get; set; }
        = Array.Empty<CreateSaleLineRequest>();

    public IReadOnlyList<CreateSalePaymentRequest> Payments { get; set; }
        = Array.Empty<CreateSalePaymentRequest>();
}