using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class SaleFinancialService :
    ISaleFinancialService
{
    private readonly InventoryPosDbContext _dbContext;

    public SaleFinancialService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SaleFinancialStateDto>
        GetStateAsync(
            Guid saleId,
            CancellationToken cancellationToken = default)
    {
        var states =
            await GetStatesAsync(
                new[] { saleId },
                cancellationToken);

        if (!states.TryGetValue(
                saleId,
                out var state))
        {
            throw new InvalidOperationException(
                "Sale was not found.");
        }

        return state;
    }

    public async Task<Dictionary<Guid, SaleFinancialStateDto>>
        GetStatesAsync(
            IEnumerable<Guid> saleIds,
            CancellationToken cancellationToken = default)
    {
        var ids =
            saleIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<
                Guid,
                SaleFinancialStateDto>();
        }

        // --------------------------------------------------------
        // ORIGINAL SALE TOTALS
        // --------------------------------------------------------

        var sales =
            await _dbContext.Sales
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    x.TotalAmount
                })
                .ToListAsync(
                    cancellationToken);

        if (sales.Count != ids.Count)
        {
            var foundIds =
                sales.Select(x => x.Id)
                    .ToHashSet();

            var missingId =
                ids.First(x =>
                    !foundIds.Contains(x));

            throw new InvalidOperationException(
                $"Sale {missingId} was not found.");
        }

        // --------------------------------------------------------
        // GROSS PAYMENTS RECEIVED
        // --------------------------------------------------------

        var paymentTotals =
            await _dbContext.SalePayments
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.SaleId))
                .GroupBy(x => x.SaleId)
                .Select(x => new
                {
                    SaleId = x.Key,
                    Amount =
                        x.Sum(y => y.Amount)
                })
                .ToDictionaryAsync(
                    x => x.SaleId,
                    x => x.Amount,
                    cancellationToken);

        // --------------------------------------------------------
        // COMPLETED RETURNS
        // --------------------------------------------------------

        var returnTotals =
            await _dbContext.SalesReturns
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.SaleId) &&
                    x.Status ==
                        SalesReturnStatus.Completed)
                .GroupBy(x => x.SaleId)
                .Select(x => new
                {
                    SaleId = x.Key,
                    Amount =
                        x.Sum(y => y.Subtotal)
                })
                .ToDictionaryAsync(
                    x => x.SaleId,
                    x => x.Amount,
                    cancellationToken);

        // --------------------------------------------------------
        // ACTUAL PAYMENT REFUNDS
        //
        // CustomerCreditAdjustment is intentionally excluded.
        // It reduces the sale obligation through the completed
        // SalesReturn itself; it is not cash/electronic money
        // leaving the business.
        // --------------------------------------------------------

        var refundTotals =
            await _dbContext.SalesRefunds
                .AsNoTracking()
                .Where(x =>
                    x.SalesReturn != null &&
                    ids.Contains(
                        x.SalesReturn.SaleId) &&
                    x.SalesReturn.Status ==
                        SalesReturnStatus.Completed &&
                    x.RefundType ==
                        SalesRefundType.PaymentRefund)
                .GroupBy(x =>
                    x.SalesReturn!.SaleId)
                .Select(x => new
                {
                    SaleId = x.Key,
                    Amount =
                        x.Sum(y => y.Amount)
                })
                .ToDictionaryAsync(
                    x => x.SaleId,
                    x => x.Amount,
                    cancellationToken);

        // --------------------------------------------------------
        // BUILD AUTHORITATIVE STATE
        // --------------------------------------------------------

        var result =
            new Dictionary<
                Guid,
                SaleFinancialStateDto>();

        foreach (var sale in sales)
        {
            var grossPaid =
                paymentTotals.TryGetValue(
                    sale.Id,
                    out var paid)
                        ? paid
                        : 0m;

            var returned =
                returnTotals.TryGetValue(
                    sale.Id,
                    out var returnedTotal)
                        ? returnedTotal
                        : 0m;

            var refunded =
                refundTotals.TryGetValue(
                    sale.Id,
                    out var refundedTotal)
                        ? refundedTotal
                        : 0m;

            ValidateTotals(
                sale.InvoiceNumber,
                sale.TotalAmount,
                grossPaid,
                returned,
                refunded);

            var effectiveTotal =
                RoundMoney(
                    Math.Max(
                        0m,
                        sale.TotalAmount -
                        returned));

            var netPaid =
                RoundMoney(
                    Math.Max(
                        0m,
                        grossPaid -
                        refunded));

            var due =
                RoundMoney(
                    Math.Max(
                        0m,
                        effectiveTotal -
                        netPaid));

            var overpayment =
                RoundMoney(
                    Math.Max(
                        0m,
                        netPaid -
                        effectiveTotal));

            result[sale.Id] =
                new SaleFinancialStateDto
                {
                    SaleId =
                        sale.Id,

                    OriginalTotal =
                        sale.TotalAmount,

                    ReturnedAmount =
                        returned,

                    EffectiveTotal =
                        effectiveTotal,

                    GrossPaidAmount =
                        grossPaid,

                    RefundedAmount =
                        refunded,

                    NetPaidAmount =
                        netPaid,

                    DueAmount =
                        due,

                    OverpaymentAmount =
                        overpayment
                };
        }

        return result;
    }

    private static void ValidateTotals(
        string invoiceNumber,
        decimal originalTotal,
        decimal grossPaid,
        decimal returned,
        decimal refunded)
    {
        if (originalTotal < 0)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceNumber} has an invalid total.");
        }

        if (grossPaid < 0)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceNumber} has an invalid payment total.");
        }

        if (returned < 0)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceNumber} has an invalid return total.");
        }

        if (refunded < 0)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceNumber} has an invalid refund total.");
        }

        if (returned > originalTotal)
        {
            throw new InvalidOperationException(
                $"Returns for invoice {invoiceNumber} exceed the original sale total.");
        }

        if (refunded > grossPaid)
        {
            throw new InvalidOperationException(
                $"Refunds for invoice {invoiceNumber} exceed payments received.");
        }
    }

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }
}