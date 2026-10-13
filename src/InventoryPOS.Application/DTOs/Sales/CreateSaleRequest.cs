namespace InventoryPOS.Application.DTOs.Sales;

public class CreateSaleRequest
{
    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public string? Notes { get; set; }

    public Guid? SalesQuoteId { get; set; }

    public decimal TransportAmount { get; set; }

    public decimal SalesTaxRate { get; set; }

    public int OrderDiscountType { get; set; } = 1;

    public decimal OrderDiscountValue { get; set; }

    public IReadOnlyList<CreateSaleLineRequest> Lines { get; set; }
        = Array.Empty<CreateSaleLineRequest>();

    public IReadOnlyList<CreateSalePaymentRequest> Payments { get; set; }
        = Array.Empty<CreateSalePaymentRequest>();

    public bool UsePricingCalculator { get; set; }
    public Guid CheckoutRequestId { get; set; }
}