
namespace InventoryPOS.Application.Pricing;

/// <summary>
/// Allocates historical order-level discount and tax across sale lines.
/// Transport is excluded from the refundable merchandise amount.
/// This class does not modify sales, returns, or inventory.
/// </summary>
public static class SalesReturnAllocationCalculator
{
    public sealed record SaleLineInput(
        Guid SaleLineId,
        decimal LineTotal);

    public sealed record SaleLineAllocation(
        Guid SaleLineId,
        decimal MerchandiseAmount,
        decimal OrderDiscountShare,
        decimal HeaderTaxShare,
        decimal RefundableTotal);

    public static IReadOnlyList<SaleLineAllocation> Allocate(
        IReadOnlyList<SaleLineInput> lines,
        decimal orderDiscountAmount,
        decimal headerTaxAmount)
    {
        if (lines is null || lines.Count == 0)
            throw new ArgumentException(
                "At least one sale line is required.",
                nameof(lines));

        if (orderDiscountAmount < 0 || headerTaxAmount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(orderDiscountAmount),
                "Discount and tax cannot be negative.");

        if (lines.Any(x => x.SaleLineId == Guid.Empty ||
                           x.LineTotal < 0))
            throw new InvalidOperationException(
                "Sale lines contain invalid values.");

        if (lines.Select(x => x.SaleLineId).Distinct().Count()
            != lines.Count)
            throw new InvalidOperationException(
                "Sale line identifiers must be unique.");

        // Stable order ensures deterministic rounding.
        var ordered = lines
            .OrderBy(x => x.SaleLineId)
            .ToList();

        var merchandiseTotal = ordered.Sum(x => x.LineTotal);

        if (orderDiscountAmount > merchandiseTotal)
            throw new InvalidOperationException(
                "Order discount exceeds merchandise value.");

        if (merchandiseTotal == 0 &&
            (orderDiscountAmount > 0 || headerTaxAmount > 0))
            throw new InvalidOperationException(
                "Cannot allocate adjustments across zero-value lines.");

        var result = new List<SaleLineAllocation>();
        decimal allocatedDiscount = 0;
        decimal allocatedTax = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            var line = ordered[i];
            var isLast = i == ordered.Count - 1;

            var discount = isLast
                ? orderDiscountAmount - allocatedDiscount
                : RoundMoney(
                    orderDiscountAmount *
                    line.LineTotal / merchandiseTotal);

            var tax = isLast
                ? headerTaxAmount - allocatedTax
                : RoundMoney(
                    headerTaxAmount *
                    line.LineTotal / merchandiseTotal);

            allocatedDiscount += discount;
            allocatedTax += tax;

            result.Add(new SaleLineAllocation(
                line.SaleLineId,
                line.LineTotal,
                discount,
                tax,
                RoundMoney(line.LineTotal - discount + tax)));
        }

        return result;
    }


    public sealed record ReturnQuantityAllocation(
        decimal Quantity,
        decimal MerchandiseAmount,
        decimal OrderDiscountAmount,
        decimal HeaderTaxAmount,
        decimal RefundableTotal);

    /// <summary>
    /// Calculates a partial return using the original line allocation.
    /// Previously returned amounts must be the actual committed amounts,
    /// not estimates. The final return absorbs rounding differences.
    /// </summary>
    public static ReturnQuantityAllocation AllocateReturnQuantity(
        SaleLineAllocation original,
        decimal soldQuantity,
        decimal returnQuantity,
        decimal previouslyReturnedQuantity,
        decimal previouslyReturnedMerchandise,
        decimal previouslyReturnedOrderDiscount,
        decimal previouslyReturnedHeaderTax)
    {
        if (soldQuantity <= 0 ||
            returnQuantity <= 0 ||
            previouslyReturnedQuantity < 0 ||
            previouslyReturnedQuantity + returnQuantity > soldQuantity)
        {
            throw new InvalidOperationException(
                "Invalid return quantity.");
        }

        if (previouslyReturnedMerchandise < 0 ||
            previouslyReturnedOrderDiscount < 0 ||
            previouslyReturnedHeaderTax < 0)
        {
            throw new InvalidOperationException(
                "Previous return amounts cannot be negative.");
        }

        if (previouslyReturnedMerchandise > original.MerchandiseAmount ||
            previouslyReturnedOrderDiscount > original.OrderDiscountShare ||
            previouslyReturnedHeaderTax > original.HeaderTaxShare)
        {
            throw new InvalidOperationException(
                "Previous returns exceed the original allocation.");
        }

        var finalReturn =
            previouslyReturnedQuantity + returnQuantity == soldQuantity;

        decimal AllocateComponent(
            decimal originalAmount,
            decimal previouslyReturned)
        {
            var amount = finalReturn
                ? originalAmount - previouslyReturned
                : RoundMoney(originalAmount * returnQuantity / soldQuantity);

            if (amount < 0 ||
                amount > originalAmount - previouslyReturned)
            {
                throw new InvalidOperationException(
                    "Return allocation exceeds the remaining amount.");
            }

            return amount;
        }

        var merchandise = AllocateComponent(
            original.MerchandiseAmount,
            previouslyReturnedMerchandise);

        var discount = AllocateComponent(
            original.OrderDiscountShare,
            previouslyReturnedOrderDiscount);

        var tax = AllocateComponent(
            original.HeaderTaxShare,
            previouslyReturnedHeaderTax);

        return new ReturnQuantityAllocation(
            returnQuantity,
            merchandise,
            discount,
            tax,
            RoundMoney(merchandise - discount + tax));
    }


    private static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
