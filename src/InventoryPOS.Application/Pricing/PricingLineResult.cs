namespace InventoryPOS.Application.Pricing;

public sealed class PricingLineResult
{
    public Guid ProductVariantId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal GrossAmount { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal NetAmount { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal LineTotal { get; init; }
}