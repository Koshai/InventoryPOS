namespace InventoryPOS.Application.DTOs.Sales;

public class CreateSaleLineRequest
{
    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }
}