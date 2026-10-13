namespace InventoryPOS.Application.Pricing;

public sealed class PricingCalculationResult
{
    public IReadOnlyList<PricingLineResult> Lines { get; init; }
        = Array.Empty<PricingLineResult>();

    public decimal GrossAmount { get; init; }

    public decimal LineDiscountAmount { get; init; }

    public decimal Subtotal { get; init; }

    public decimal OrderDiscountAmount { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal TaxableAmount { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal TransportAmount { get; init; }

    public decimal TotalAmount { get; init; }
}