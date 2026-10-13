namespace InventoryPOS.Application.Pricing;

public sealed class PricingLineRequest
{
    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public PricingAdjustmentType DiscountType { get; set; }
        = PricingAdjustmentType.FixedAmount;

    public decimal DiscountValue { get; set; }

    public decimal TaxRate { get; set; }
}