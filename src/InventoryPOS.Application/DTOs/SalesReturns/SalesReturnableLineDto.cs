namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnableLineDto
{
    public Guid SaleLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; }
        = string.Empty;

    public string Sku { get; set; }
        = string.Empty;

    public decimal SoldQuantity { get; set; }

    public decimal ReturnedQuantity { get; set; }

    public decimal ReturnableQuantity =>
        Math.Max(
            0m,
            SoldQuantity - ReturnedQuantity);

    public decimal UnitPrice { get; set; }

    public decimal OriginalDiscountAmount { get; set; }

    public decimal OriginalTaxAmount { get; set; }

    public decimal OriginalLineTotal { get; set; }
}