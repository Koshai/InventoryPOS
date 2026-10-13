namespace InventoryPOS.Application.Pricing;

public sealed class PricingCalculationRequest
{
    public IReadOnlyCollection<PricingLineRequest> Lines { get; set; }
        = Array.Empty<PricingLineRequest>();

    public PricingAdjustmentType OrderDiscountType { get; set; }
        = PricingAdjustmentType.FixedAmount;

    public decimal OrderDiscountValue { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TransportAmount { get; set; }
}