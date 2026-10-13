namespace InventoryPOS.Application.Pricing;

public sealed class PricingCalculator
    : IPricingCalculator
{
    public PricingCalculationResult Calculate(
        PricingCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var lineResults =
            new List<PricingLineResult>();

        decimal grossAmount = 0m;
        decimal lineDiscountAmount = 0m;
        decimal subtotal = 0m;
        decimal lineTaxAmount = 0m;

        foreach (var line in request.Lines)
        {
            var gross =
                RoundMoney(
                    line.Quantity *
                    line.UnitPrice);

            var discount =
                CalculateAdjustment(
                    gross,
                    line.DiscountType,
                    line.DiscountValue);

            var net =
                RoundMoney(
                    gross - discount);

            var lineTax =
                line.TaxRate > 0m
                    ? CalculatePercentage(
                        net,
                        line.TaxRate)
                    : 0m;

            var lineTotal =
                RoundMoney(
                    net + lineTax);

            lineResults.Add(
                new PricingLineResult
                {
                    ProductVariantId =
                        line.ProductVariantId,

                    Quantity =
                        line.Quantity,

                    UnitPrice =
                        line.UnitPrice,

                    GrossAmount =
                        gross,

                    DiscountAmount =
                        discount,

                    NetAmount =
                        net,

                    TaxAmount =
                        lineTax,

                    LineTotal =
                        lineTotal
                });

            grossAmount += gross;
            lineDiscountAmount += discount;
            subtotal += net;
            lineTaxAmount += lineTax;
        }

        grossAmount =
            RoundMoney(grossAmount);

        lineDiscountAmount =
            RoundMoney(lineDiscountAmount);

        subtotal =
            RoundMoney(subtotal);

        lineTaxAmount =
            RoundMoney(lineTaxAmount);

        var orderDiscount =
            CalculateAdjustment(
                subtotal,
                request.OrderDiscountType,
                request.OrderDiscountValue);

        var taxableAmount =
            RoundMoney(
                subtotal - orderDiscount);

        decimal taxAmount;

        if (request.TaxRate > 0m)
        {
            taxAmount =
                CalculatePercentage(
                    taxableAmount,
                    request.TaxRate);
        }
        else
        {
            taxAmount =
                lineTaxAmount;
        }

        var transport =
            RoundMoney(
                request.TransportAmount);

        var total =
            RoundMoney(
                taxableAmount +
                taxAmount +
                transport);

        return new PricingCalculationResult
        {
            Lines =
                lineResults,

            GrossAmount =
                grossAmount,

            LineDiscountAmount =
                lineDiscountAmount,

            Subtotal =
                subtotal,

            OrderDiscountAmount =
                orderDiscount,

            DiscountAmount =
                RoundMoney(
                    lineDiscountAmount +
                    orderDiscount),

            TaxableAmount =
                taxableAmount,

            TaxAmount =
                taxAmount,

            TransportAmount =
                transport,

            TotalAmount =
                total
        };
    }

    private static decimal CalculateAdjustment(
        decimal baseAmount,
        PricingAdjustmentType type,
        decimal value)
    {
        if (value <= 0m)
        {
            return 0m;
        }

        decimal amount =
            type switch
            {
                PricingAdjustmentType.FixedAmount =>
                    value,

                PricingAdjustmentType.Percentage =>
                    baseAmount *
                    value /
                    100m,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        "Unsupported pricing adjustment type.")
            };

        amount =
            RoundMoney(amount);

        if (amount > baseAmount)
        {
            throw new InvalidOperationException(
                "Discount cannot exceed the amount being discounted.");
        }

        return amount;
    }

    private static decimal CalculatePercentage(
        decimal baseAmount,
        decimal rate)
    {
        return RoundMoney(
            baseAmount *
            rate /
            100m);
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static void ValidateRequest(
        PricingCalculationRequest request)
    {
        if (request.Lines is null)
        {
            throw new ArgumentException(
                "Pricing lines are required.",
                nameof(request));
        }

        if (request.OrderDiscountValue < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.OrderDiscountValue),
                "Order discount cannot be negative.");
        }

        if (request.TaxRate < 0m ||
            request.TaxRate > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TaxRate),
                "Tax rate must be between 0 and 100.");
        }

        if (request.TransportAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TransportAmount),
                "Transport amount cannot be negative.");
        }

        foreach (var line in request.Lines)
        {
            if (line.ProductVariantId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Product variant is required.");
            }

            if (line.Quantity <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line.Quantity),
                    "Quantity must be greater than zero.");
            }

            if (line.UnitPrice < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line.UnitPrice),
                    "Unit price cannot be negative.");
            }

            if (line.DiscountValue < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line.DiscountValue),
                    "Line discount cannot be negative.");
            }

            if (line.TaxRate < 0m ||
                line.TaxRate > 100m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line.TaxRate),
                    "Line tax rate must be between 0 and 100.");
            }

            if (line.DiscountType ==
                    PricingAdjustmentType.Percentage &&
                line.DiscountValue > 100m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line.DiscountValue),
                    "Percentage discount cannot exceed 100.");
            }
        }

        if (request.OrderDiscountType ==
                PricingAdjustmentType.Percentage &&
            request.OrderDiscountValue > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.OrderDiscountValue),
                "Percentage discount cannot exceed 100.");
        }

        var hasLineTax =
            request.Lines.Any(
                x => x.TaxRate > 0m);

        if (hasLineTax &&
            request.TaxRate > 0m)
        {
            throw new InvalidOperationException(
                "Line-level tax and order-level tax cannot be applied together.");
        }
    }
}