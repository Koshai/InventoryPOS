namespace InventoryPOS.Application.Pricing;

public interface IPricingCalculator
{
    PricingCalculationResult Calculate(
        PricingCalculationRequest request);
}