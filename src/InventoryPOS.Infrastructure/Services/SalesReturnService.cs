using InventoryPOS.Application.DTOs.SalesReturns;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Pricing;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryPOS.Infrastructure.Services;

public sealed class SalesReturnService :
    ISalesReturnService
{
    private readonly InventoryPosDbContext _dbContext;
    private readonly ISaleFinancialService _financialService;

    public SalesReturnService(
        InventoryPosDbContext dbContext,
        ISaleFinancialService financialService)
    {
        _dbContext = dbContext;
        _financialService = financialService;
    }

    // ============================================================
    // GET RETURNABLE SALE
    // ============================================================

    public async Task<SalesReturnableSaleDto?>
        GetReturnableSaleAsync(
            Guid saleId,
            CancellationToken cancellationToken = default)
    {
        var sale =
            await _dbContext.Sales
                .AsNoTracking()
                .Where(x =>
                    x.Id == saleId)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    x.SaleDate,
                    x.LocationId,
                    x.CustomerId,
                    x.Status,
                    x.OrderDiscountAmount,
                    x.TaxAmount,

                    CustomerName =
                        x.Customer != null
                            ? x.Customer.Name
                            : "Walk-in Customer",

                    Lines =
                        x.Lines.Select(line => new
                        {
                            line.Id,
                            line.ProductVariantId,
                            line.Quantity,
                            line.UnitPrice,
                            line.DiscountAmount,
                            line.TaxAmount,
                            line.LineTotal,

                            ProductName =
                                line.ProductVariant != null &&
                                line.ProductVariant.Product != null
                                    ? line.ProductVariant.Product.Name
                                    : string.Empty,

                            Sku =
                                line.ProductVariant != null
                                    ? line.ProductVariant.SKU
                                    : string.Empty
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (sale is null)
            return null;

        EnsureSaleCanBeReturned(
            sale.Status);

        var state =
            await _financialService.GetStateAsync(
                sale.Id,
                cancellationToken);

        var saleLineIds =
            sale.Lines
                .Select(x => x.Id)
                .ToList();

        var returnedQuantities =
            await _dbContext.SalesReturnLines
                .AsNoTracking()
                .Where(x =>
                    saleLineIds.Contains(
                        x.SaleLineId) &&
                    x.SalesReturn != null &&
                    x.SalesReturn.Status ==
                        SalesReturnStatus.Completed)
                .GroupBy(x =>
                    x.SaleLineId)
                .Select(x => new
                {
                    SaleLineId =
                        x.Key,

                    Quantity =
                        x.Sum(y => y.Quantity),

                    LineTotal =
                        x.Sum(y => y.LineTotal)
                })
                .ToDictionaryAsync(
                    x => x.SaleLineId,
                    x => new
                    {
                        x.Quantity,
                        x.LineTotal
                    },
                    cancellationToken);

        var originalAllocations =
            SalesReturnAllocationCalculator.Allocate(
                sale.Lines
                    .Select(x =>
                        new SalesReturnAllocationCalculator.SaleLineInput(
                            x.Id,
                            x.LineTotal))
                    .ToList(),
                sale.OrderDiscountAmount,
                sale.TaxAmount)
            .ToDictionary(x => x.SaleLineId);

        var result =
            new SalesReturnableSaleDto
            {
                SaleId =
                    sale.Id,

                InvoiceNumber =
                    sale.InvoiceNumber,

                SaleDate =
                    sale.SaleDate,

                LocationId =
                    sale.LocationId,

                CustomerId =
                    sale.CustomerId,

                CustomerName =
                    sale.CustomerName,

                SaleStatus =
                    sale.Status,

                TotalAmount =
                    state.EffectiveTotal,

                PaidAmount =
                    state.NetPaidAmount,

                DueAmount =
                    state.DueAmount
            };

        foreach (var line in sale.Lines)
        {
            var previous =
                returnedQuantities.TryGetValue(
                    line.Id,
                    out var totals)
                        ? totals
                        : null;

            var returnedQuantity =
                previous?.Quantity ?? 0m;

            var previouslyReturnedTotal =
                previous?.LineTotal ?? 0m;

            result.Lines.Add(
                new SalesReturnableLineDto
                {
                    SaleLineId =
                        line.Id,

                    ProductVariantId =
                        line.ProductVariantId,

                    ProductName =
                        line.ProductName,

                    Sku =
                        line.Sku,

                    SoldQuantity =
                        line.Quantity,

                    ReturnedQuantity =
                        returnedQuantity,

                    UnitPrice =
                        line.UnitPrice,

                    OriginalDiscountAmount =
                        line.DiscountAmount,

                    OriginalTaxAmount =
                        line.TaxAmount,

                    OriginalLineTotal =
                        line.LineTotal,

                    OriginalRefundableTotal =
                        originalAllocations[line.Id].RefundableTotal,

                    PreviouslyReturnedTotal =
                        previouslyReturnedTotal
                });
        }

        return result;
    }

    // ============================================================
    // GET RETURN HISTORY
    // ============================================================

    public async Task<SalesReturnHistoryResultDto>
        GetReturnHistoryAsync(
            SalesReturnHistoryQueryDto request,
            CancellationToken cancellationToken = default)
    {
        var pageNumber =
            Math.Max(1, request.PageNumber);

        var pageSize =
            Math.Clamp(
                request.PageSize,
                1,
                100);

        var query =
            _dbContext.SalesReturns
                .AsNoTracking()
                .AsQueryable();

        // --------------------------------------------------------
        // SEARCH
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                request.SearchText))
        {
            var search =
                request.SearchText.Trim();

            query =
                query.Where(x =>
                    EF.Functions.ILike(
                        x.ReturnNumber,
                        $"%{search}%") ||

                    (
                        x.Sale != null &&
                        EF.Functions.ILike(
                            x.Sale.InvoiceNumber,
                            $"%{search}%")
                    ) ||

                    (
                        x.Customer != null &&
                        EF.Functions.ILike(
                            x.Customer.Name,
                            $"%{search}%")
                    ));
        }

        // --------------------------------------------------------
        // STATUS
        // --------------------------------------------------------

        if (request.Status.HasValue)
        {
            query =
                query.Where(x =>
                    x.Status ==
                    request.Status.Value);
        }

        // --------------------------------------------------------
        // LOCATION
        // --------------------------------------------------------

        if (request.LocationId.HasValue &&
            request.LocationId.Value != Guid.Empty)
        {
            query =
                query.Where(x =>
                    x.LocationId ==
                    request.LocationId.Value);
        }

        // --------------------------------------------------------
        // DATE RANGE
        // --------------------------------------------------------

        if (request.FromDate.HasValue)
        {
            query =
                query.Where(x =>
                    x.ReturnDate >=
                    request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            /*
             * Treat ToDate as an inclusive calendar date.
             *
             * Example:
             * 2026-09-25 means everything before
             * 2026-09-26 00:00.
             */
            var exclusiveTo =
                request.ToDate.Value.Date
                    .AddDays(1);

            query =
                query.Where(x =>
                    x.ReturnDate <
                    exclusiveTo);
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderByDescending(x =>
                    x.ReturnDate)
                .ThenByDescending(x =>
                    x.ReturnNumber)
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(pageSize)
                .Select(x =>
                    new SalesReturnHistoryItemDto
                    {
                        SalesReturnId =
                            x.Id,

                        ReturnNumber =
                            x.ReturnNumber,

                        ReturnDate =
                            x.ReturnDate,

                        SaleId =
                            x.SaleId,

                        InvoiceNumber =
                            x.Sale != null
                                ? x.Sale.InvoiceNumber
                                : string.Empty,

                        CustomerId =
                            x.CustomerId,

                        CustomerName =
                            x.Customer != null
                                ? x.Customer.Name
                                : "Walk-in Customer",

                        LocationId =
                            x.LocationId,

                        LocationName =
                            x.Location != null
                                ? x.Location.Name
                                : string.Empty,

                        ReturnAmount =
                            x.Subtotal,

                        CreditAdjustmentAmount =
                            x.CreditAdjustmentAmount,

                        RefundedAmount =
                            x.RefundAmount,

                        Status =
                            x.Status
                    })
                .ToListAsync(
                    cancellationToken);

        return new SalesReturnHistoryResultDto
        {
            Items =
                items,

            TotalCount =
                totalCount,

            PageNumber =
                pageNumber,

            PageSize =
                pageSize
        };
    }

    // ============================================================
    // GET RETURN DETAILS
    // ============================================================

    public async Task<SalesReturnDetailsDto?>
        GetReturnDetailsAsync(
            Guid salesReturnId,
            CancellationToken cancellationToken = default)
    {
        if (salesReturnId == Guid.Empty)
            return null;

        var result =
            await _dbContext.SalesReturns
                .AsNoTracking()
                .Where(x =>
                    x.Id ==
                    salesReturnId)
                .Select(x =>
                    new SalesReturnDetailsDto
                    {
                        SalesReturnId =
                            x.Id,

                        ReturnNumber =
                            x.ReturnNumber,

                        ReturnDate =
                            x.ReturnDate,

                        Status =
                            x.Status,

                        SaleId =
                            x.SaleId,

                        InvoiceNumber =
                            x.Sale != null
                                ? x.Sale.InvoiceNumber
                                : string.Empty,

                        SaleDate =
                            x.Sale != null
                                ? x.Sale.SaleDate
                                : default,

                        LocationId =
                            x.LocationId,

                        LocationName =
                            x.Location != null
                                ? x.Location.Name
                                : string.Empty,

                        CustomerId =
                            x.CustomerId,

                        CustomerName =
                            x.Customer != null
                                ? x.Customer.Name
                                : "Walk-in Customer",

                        OriginalSaleTotal =
                            x.Sale != null
                                ? x.Sale.TotalAmount
                                : 0m,

                        ReturnAmount =
                            x.Subtotal,

                        CreditAdjustmentAmount =
                            x.CreditAdjustmentAmount,

                        RefundedAmount =
                            x.RefundAmount,

                        Reason =
                            x.Reason,

                        Notes =
                            x.Notes,

                        Lines =
                            x.Lines
                                .OrderBy(line =>
                                    line.CreatedAtUtc)
                                .Select(line =>
                                    new SalesReturnDetailsLineDto
                                    {
                                        SalesReturnLineId =
                                            line.Id,

                                        SaleLineId =
                                            line.SaleLineId,

                                        ProductVariantId =
                                            line.ProductVariantId,

                                        ProductName =
                                            line.ProductVariant != null &&
                                            line.ProductVariant.Product != null
                                                ? line.ProductVariant.Product.Name
                                                : string.Empty,

                                        SKU =
                                            line.ProductVariant != null
                                                ? line.ProductVariant.SKU
                                                : string.Empty,

                                        Quantity =
                                            line.Quantity,

                                        UnitPrice =
                                            line.UnitPrice,

                                        DiscountAmount =
                                            line.DiscountAmount,

                                        TaxAmount =
                                            line.TaxAmount,

                                        LineTotal =
                                            line.LineTotal,

                                        IsRestockable =
                                            line.IsRestockable,

                                        Reason =
                                            line.Reason
                                    })
                                .ToList(),

                        Refunds =
                            x.Refunds
                                .OrderBy(refund =>
                                    refund.RefundDate)
                                .ThenBy(refund =>
                                    refund.CreatedAtUtc)
                                .Select(refund =>
                                    new SalesReturnRefundDto
                                    {
                                        SalesRefundId =
                                            refund.Id,

                                        RefundType =
                                            refund.RefundType,

                                        PaymentMethod =
                                            refund.PaymentMethod,

                                        Amount =
                                            refund.Amount,

                                        RefundDate =
                                            refund.RefundDate,

                                        ReferenceNumber =
                                            refund.ReferenceNumber,

                                        Notes =
                                            refund.Notes
                                    })
                                .ToList()
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);

        return result;
    }

    // ============================================================
    // CREATE RETURN
    // ============================================================

    public async Task<CreateSalesReturnResultDto>
        CreateReturnAsync(
            CreateSalesReturnRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(
            request);

        var requestFingerprint = ComputeReturnRequestFingerprint(request);


        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            // DT6.4.1:
            // Serialize returns for the same sale across all
            // terminals connected to the PostgreSQL database.
            //
            // Lock must be acquired BEFORE loading the sale,
            // financial state, or previous return quantities.

            var lockKey =
                $"InventoryPOS:SalesReturn:Sale:{request.SaleId:D}";

            await _dbContext.Database
                .SqlQuery<int>(
                    $"""
            SELECT 1 AS "Value"
            FROM (
                SELECT pg_advisory_xact_lock(
                    hashtextextended({lockKey}, 0)
                )
            ) AS sales_return_lock
            """)
                .SingleAsync(cancellationToken);

            if (request.ReturnRequestId != Guid.Empty)
            {
                var existingReturn = await _dbContext.SalesReturns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.ReturnRequestId == request.ReturnRequestId,
                        cancellationToken);

                if (existingReturn is not null)
                {
                    if (existingReturn.ReturnRequestFingerprint !=
                        requestFingerprint)
                    {
                        throw new InvalidOperationException(
                            "This return request ID has already been used " +
                            "with different return details.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            existingReturn.ReturnResultJson))
                    {
                        throw new InvalidOperationException(
                            "The original return result is unavailable. " +
                            "Do not resubmit this return automatically.");
                    }

                    var originalResult =
                        JsonSerializer.Deserialize<CreateSalesReturnResultDto>(
                            existingReturn.ReturnResultJson);

                    return originalResult
                        ?? throw new InvalidOperationException(
                            "The saved return result could not be restored.");
                }
            }

            var sale =
                await _dbContext.Sales
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            request.SaleId,
                        cancellationToken);

            if (sale is null)
            {
                throw new InvalidOperationException(
                    "Sale was not found.");
            }

            EnsureSaleCanBeReturned(
                sale.Status);

            var financialStateBefore =
                await _financialService.GetStateAsync(
                    sale.Id,
                    cancellationToken);

            var duplicateLine =
                request.Lines
                    .GroupBy(x =>
                        x.SaleLineId)
                    .FirstOrDefault(x =>
                        x.Count() > 1);

            if (duplicateLine is not null)
            {
                throw new InvalidOperationException(
                    "The same sale line cannot appear more than once in a return.");
            }

            var requestedLineIds =
                request.Lines
                    .Select(x =>
                        x.SaleLineId)
                    .ToList();

            var saleLines =
                sale.Lines
                    .Where(x =>
                        requestedLineIds.Contains(
                            x.Id))
                    .ToDictionary(x => x.Id);

            if (saleLines.Count !=
                request.Lines.Count)
            {
                throw new InvalidOperationException(
                    "One or more selected lines do not belong to this sale.");
            }

            var previousReturnRows =
                await _dbContext.SalesReturnLines
                    .AsNoTracking()
                    .Where(x =>
                        requestedLineIds.Contains(
                            x.SaleLineId) &&
                        x.SalesReturn != null &&
                        x.SalesReturn.Status ==
                            SalesReturnStatus.Completed)
                    .GroupBy(x =>
                        x.SaleLineId)
                    .Select(x => new
                    {
                        SaleLineId =
                            x.Key,

                        Quantity =
                            x.Sum(y =>
                                y.Quantity),

                        LineTotal =
                            x.Sum(y =>
                                y.LineTotal),

                        DiscountAmount =
                            x.Sum(y =>
                                y.DiscountAmount),

                        TaxAmount =
                            x.Sum(y =>
                                y.TaxAmount)
                    })
                    .ToListAsync(
                        cancellationToken);

            var previouslyReturned =
                previousReturnRows
                    .ToDictionary(
                        x => x.SaleLineId,
                        x => new PreviousReturnTotals
                        {
                            Quantity =
                                x.Quantity,

                            LineTotal =
                                x.LineTotal,

                            DiscountAmount =
                                x.DiscountAmount,

                            TaxAmount =
                                x.TaxAmount
                        });


            /*
             * DT6.3C.3
             * Allocate the original saved order-level discount
             * and VAT across all original sale lines.
             *
             * Transport is nonrefundable.
             * Completed historical returns remain unchanged.
             */
            var originalAllocations =
                SalesReturnAllocationCalculator.Allocate(
                    sale.Lines
                        .Select(x =>
                            new SalesReturnAllocationCalculator.SaleLineInput(
                                x.Id,
                                x.LineTotal))
                        .ToList(),
                    sale.OrderDiscountAmount,
                    sale.TaxAmount)
                .ToDictionary(x => x.SaleLineId);


            var salesReturn =
                new SalesReturn
                {
                    ReturnNumber =
                        await GenerateReturnNumberAsync(
                            cancellationToken),

                    SaleId =
                        sale.Id,

                    LocationId =
                        sale.LocationId,

                    CustomerId =
                        sale.CustomerId,

                    ReturnDate =
                        request.ReturnDate,

                    Status =
                        SalesReturnStatus.Completed,

                    Reason =
                        Clean(
                            request.Reason),

                    Notes =
                        Clean(
                            request.Notes),

                    ReturnRequestId =
                        request.ReturnRequestId == Guid.Empty
                            ? null
                            : request.ReturnRequestId,

                    ReturnRequestFingerprint =
                        request.ReturnRequestId == Guid.Empty
                            ? null
                            : requestFingerprint,
                };

            decimal returnTotal = 0m;

            var result =
                new CreateSalesReturnResultDto
                {
                    SalesReturnId =
                        salesReturn.Id,

                    ReturnNumber =
                        salesReturn.ReturnNumber
                };

            foreach (var requestedLine
                     in request.Lines)
            {
                var original =
                    saleLines[
                        requestedLine.SaleLineId];

                var previous =
                    previouslyReturned.TryGetValue(
                        original.Id,
                        out var previousTotals)
                            ? previousTotals
                            : null;

                var previousQuantity =
                    previous?.Quantity ??
                    0m;

                var returnableQuantity =
                    original.Quantity -
                    previousQuantity;

                if (returnableQuantity <= 0)
                {
                    throw new InvalidOperationException(
                        $"Sale line {original.Id} has already been fully returned.");
                }

                if (requestedLine.Quantity >
                    returnableQuantity)
                {
                    throw new InvalidOperationException(
                        $"Return quantity {requestedLine.Quantity:N3} exceeds the remaining returnable quantity of {returnableQuantity:N3}.");
                }


                var isFinalReturn =
                    requestedLine.Quantity ==
                    returnableQuantity;

                var allocation =
                    originalAllocations[original.Id];

                var previousReturnTotal =
                    previous?.LineTotal ?? 0m;

                var remainingRefundable =
                    RoundMoney(
                        allocation.RefundableTotal -
                        previousReturnTotal);

                if (remainingRefundable < 0m)
                {
                    throw new InvalidOperationException(
                        "Previous returns exceed the original refundable value.");
                }

                /*
                 * For partial returns, calculate the proportional
                 * amount from the original historical allocation.
                 *
                 * For the final return, use the remaining balance
                 * to absorb rounding differences and preserve
                 * amounts already committed.
                 */
                var returnLineTotal =
                    isFinalReturn
                        ? remainingRefundable
                        : RoundMoney(
                            allocation.RefundableTotal *
                            requestedLine.Quantity /
                            original.Quantity);

                if (returnLineTotal < 0m ||
                    returnLineTotal > remainingRefundable)
                {
                    throw new InvalidOperationException(
                        "Return amount exceeds the remaining refundable value.");
                }

                /*
                 * Preserve the existing line-discount calculation.
                 * Add the original header VAT allocation to the
                 * returned tax component.
                 */
                var existingFinancials =
                    CalculateReturnLineFinancials(
                        original,
                        requestedLine.Quantity,
                        previous,
                        isFinalReturn);

                var originalAllocatedTax =
                    original.TaxAmount +
                    allocation.HeaderTaxShare;

                var previousReturnedTax =
                    previous?.TaxAmount ?? 0m;

                var returnTax =
                    isFinalReturn
                        ? RoundMoney(
                            originalAllocatedTax -
                            previousReturnedTax)
                        : RoundMoney(
                            originalAllocatedTax *
                            requestedLine.Quantity /
                            original.Quantity);

                if (returnTax < 0m ||
                    returnTax > RoundMoney(
                        originalAllocatedTax -
                        previousReturnedTax))
                {
                    throw new InvalidOperationException(
                        "Return tax exceeds the remaining allocated tax.");
                }

                var financials =
                    new ReturnLineFinancials
                    {
                        DiscountAmount =
                            existingFinancials.DiscountAmount,

                        TaxAmount =
                            returnTax,

                        LineTotal =
                            returnLineTotal
                    };


                var returnLine =
                    new SalesReturnLine
                    {
                        SaleLineId =
                            original.Id,

                        ProductVariantId =
                            original.ProductVariantId,

                        Quantity =
                            requestedLine.Quantity,

                        UnitPrice =
                            original.UnitPrice,

                        DiscountAmount =
                            financials.DiscountAmount,

                        TaxAmount =
                            financials.TaxAmount,

                        LineTotal =
                            financials.LineTotal,

                        IsRestockable =
                            requestedLine.IsRestockable,

                        Reason =
                            Clean(
                                requestedLine.Reason)
                    };

                salesReturn.Lines.Add(
                    returnLine);

                returnTotal +=
                    financials.LineTotal;

                result.Lines.Add(
                    new CreateSalesReturnLineResultDto
                    {
                        SaleLineId =
                            original.Id,

                        ProductVariantId =
                            original.ProductVariantId,

                        Quantity =
                            requestedLine.Quantity,

                        LineTotal =
                            financials.LineTotal,

                        IsRestockable =
                            requestedLine.IsRestockable
                    });
            }

            returnTotal =
                RoundMoney(
                    returnTotal);

            // ----------------------------------------------------
            // FINANCIAL EFFECT
            // ----------------------------------------------------

            var dueBeforeReturn =
                financialStateBefore.DueAmount;

            var creditAdjustment =
                RoundMoney(
                    Math.Min(
                        dueBeforeReturn,
                        returnTotal));

            var effectiveTotalAfterReturn =
                RoundMoney(
                    Math.Max(
                        0m,
                        financialStateBefore.EffectiveTotal -
                        returnTotal));

            /*
             * NetPaidAmount already subtracts all previous
             * physical refunds.
             */
            var overpaymentAfterReturnBeforeNewRefund =
                RoundMoney(
                    Math.Max(
                        0m,
                        financialStateBefore.NetPaidAmount -
                        effectiveTotalAfterReturn));

            /*
             * The current return cannot refund more than the
             * financial portion not used to cancel debt.
             */
            var returnRefundCapacity =
                RoundMoney(
                    Math.Max(
                        0m,
                        returnTotal -
                        creditAdjustment));

            var refundableAmount =
                RoundMoney(
                    Math.Min(
                        overpaymentAfterReturnBeforeNewRefund,
                        returnRefundCapacity));

            ValidateRefundRequest(
                request,
                refundableAmount);

            var requestedRefund =
                RoundMoney(
                    request.RefundAmount);

            var netPaidAfterReturn =
                RoundMoney(
                    Math.Max(
                        0m,
                        financialStateBefore.NetPaidAmount -
                        requestedRefund));

            var dueAfterReturn =
                RoundMoney(
                    Math.Max(
                        0m,
                        effectiveTotalAfterReturn -
                        netPaidAfterReturn));

            salesReturn.Subtotal =
                returnTotal;

            salesReturn.CreditAdjustmentAmount =
                creditAdjustment;

            salesReturn.RefundAmount =
                requestedRefund;

            if (creditAdjustment > 0)
            {
                salesReturn.Refunds.Add(
                    new SalesRefund
                    {
                        RefundType =
                            SalesRefundType.CustomerCreditAdjustment,

                        PaymentMethod =
                            null,

                        Amount =
                            creditAdjustment,

                        RefundDate =
                            request.ReturnDate,

                        ReferenceNumber =
                            null,

                        Notes =
                            "Applied against outstanding customer balance."
                    });
            }

            if (requestedRefund > 0)
            {
                salesReturn.Refunds.Add(
                    new SalesRefund
                    {
                        RefundType =
                            SalesRefundType.PaymentRefund,

                        PaymentMethod =
                            request.RefundPaymentMethod,

                        Amount =
                            requestedRefund,

                        RefundDate =
                            request.ReturnDate,

                        ReferenceNumber =
                            Clean(
                                request.RefundReferenceNumber),

                        Notes =
                            Clean(
                                request.RefundNotes)
                    });
            }

            /*
             * Sale fields are cached summaries.
             *
             * Financial records remain authoritative:
             * SalePayment + completed SalesReturn +
             * PaymentRefund.
             */
            sale.PaidAmount =
                netPaidAfterReturn;

            sale.DueAmount =
                dueAfterReturn;

            ApplySaleStatus(
                sale,
                netPaidAfterReturn,
                dueAfterReturn);

            _dbContext.SalesReturns.Add(
                salesReturn);

            await RestoreReturnedInventoryAsync(
                sale,
                salesReturn,
                cancellationToken);

            result.ReturnAmount = returnTotal;

            result.CreditAdjustmentAmount = creditAdjustment;

            result.RefundableAmount = refundableAmount;

            result.RefundedAmount = requestedRefund;

            result.RemainingRefundableAmount =
                RoundMoney(refundableAmount - requestedRefund);

            result.RemainingSaleDue = dueAfterReturn;

            // Store the exact original result in the same transaction.
            if (request.ReturnRequestId != Guid.Empty)
            {
                salesReturn.ReturnResultJson =
                    JsonSerializer.Serialize(result);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    // ============================================================
    // INVENTORY RESTORATION
    // ============================================================

    private async Task RestoreReturnedInventoryAsync(
        Sale originalSale,
        SalesReturn salesReturn,
        CancellationToken cancellationToken)
    {
        var restockableLines =
            salesReturn.Lines
                .Where(x =>
                    x.IsRestockable &&
                    x.Quantity > 0)
                .ToList();

        if (restockableLines.Count == 0)
            return;

        foreach (var returnLine
                 in restockableLines)
        {
            await RestoreReturnLineInventoryAsync(
                originalSale,
                salesReturn,
                returnLine,
                cancellationToken);
        }
    }

    private async Task RestoreReturnLineInventoryAsync(
        Sale originalSale,
        SalesReturn salesReturn,
        SalesReturnLine returnLine,
        CancellationToken cancellationToken)
    {
        var quantityToRestore =
            returnLine.Quantity;

        var originalMovements =
            await _dbContext.StockMovements
                .AsNoTracking()
                .Where(x =>
                    x.ReferenceId ==
                        originalSale.Id &&
                    x.MovementType ==
                        InventoryMovementType.Sale &&
                    x.ProductVariantId ==
                        returnLine.ProductVariantId &&
                    x.LocationId ==
                        originalSale.LocationId)
                .OrderBy(x =>
                    x.CreatedAtUtc)
                .ThenBy(x =>
                    x.Id)
                .ToListAsync(
                    cancellationToken);

        if (originalMovements.Count == 0)
        {
            throw new InvalidOperationException(
                $"No original stock movements were found for product variant {returnLine.ProductVariantId}.");
        }

        var completedReturnIds =
            await _dbContext.SalesReturns
                .AsNoTracking()
                .Where(x =>
                    x.SaleId ==
                        originalSale.Id &&
                    x.Status ==
                        SalesReturnStatus.Completed)
                .Select(x =>
                    x.Id)
                .ToListAsync(
                    cancellationToken);

        var previousReturnMovements =
            await _dbContext.StockMovements
                .AsNoTracking()
                .Where(x =>
                    x.MovementType ==
                        InventoryMovementType.CustomerReturn &&
                    x.ProductVariantId ==
                        returnLine.ProductVariantId &&
                    x.LocationId ==
                        originalSale.LocationId &&
                    x.ReferenceType ==
                        "SalesReturn" &&
                    x.ReferenceId.HasValue &&
                    completedReturnIds.Contains(
                        x.ReferenceId.Value))
                .ToListAsync(
                    cancellationToken);

        var previouslyRestoredByBatch =
            previousReturnMovements
                .Where(x =>
                    x.InventoryBatchId.HasValue)
                .GroupBy(x =>
                    x.InventoryBatchId!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(y =>
                        y.Quantity));

        foreach (var movement
                 in originalMovements.Where(
                     x =>
                         x.InventoryBatchId.HasValue))
        {
            if (quantityToRestore <= 0)
                break;

            var batchId =
                movement.InventoryBatchId!.Value;

            var originallyConsumed =
                Math.Abs(
                    movement.Quantity);

            var previouslyRestored =
                previouslyRestoredByBatch
                    .TryGetValue(
                        batchId,
                        out var restored)
                            ? restored
                            : 0m;

            var remainingRestorable =
                Math.Max(
                    0m,
                    originallyConsumed -
                    previouslyRestored);

            if (remainingRestorable <= 0)
                continue;

            var restoreQuantity =
                Math.Min(
                    quantityToRestore,
                    remainingRestorable);

            var batch =
                await _dbContext.InventoryBatches
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            batchId,
                        cancellationToken);

            if (batch is null)
            {
                throw new InvalidOperationException(
                    $"Inventory batch {batchId} used by the original sale could not be found.");
            }

            batch.QuantityOnHand +=
                restoreQuantity;

            _dbContext.StockMovements.Add(
                new StockMovement
                {
                    ProductVariantId =
                        returnLine.ProductVariantId,

                    LocationId =
                        originalSale.LocationId,

                    MovementType =
                        InventoryMovementType.CustomerReturn,

                    Quantity =
                        restoreQuantity,

                    UnitCost =
                        movement.UnitCost,

                    ReferenceType =
                        "SalesReturn",

                    ReferenceId =
                        salesReturn.Id,

                    InventoryBatchId =
                        batch.Id,

                    Notes =
                        $"Customer return {salesReturn.ReturnNumber}"
                });

            quantityToRestore -=
                restoreQuantity;
        }

        // --------------------------------------------------------
        // LEGACY / UNBATCHED STOCK
        // --------------------------------------------------------

        if (quantityToRestore > 0)
        {
            var originalUnbatchedQuantity =
                originalMovements
                    .Where(x =>
                        !x.InventoryBatchId.HasValue)
                    .Sum(x =>
                        Math.Abs(
                            x.Quantity));

            var previouslyRestoredUnbatched =
                previousReturnMovements
                    .Where(x =>
                        !x.InventoryBatchId.HasValue)
                    .Sum(x =>
                        x.Quantity);

            var remainingUnbatchedRestorable =
                Math.Max(
                    0m,
                    originalUnbatchedQuantity -
                    previouslyRestoredUnbatched);

            var unbatchedRestoreQuantity =
                Math.Min(
                    quantityToRestore,
                    remainingUnbatchedRestorable);

            if (unbatchedRestoreQuantity > 0)
            {
                _dbContext.StockMovements.Add(
                    new StockMovement
                    {
                        ProductVariantId =
                            returnLine.ProductVariantId,

                        LocationId =
                            originalSale.LocationId,

                        MovementType =
                            InventoryMovementType.CustomerReturn,

                        Quantity =
                            unbatchedRestoreQuantity,

                        UnitCost =
                            null,

                        ReferenceType =
                            "SalesReturn",

                        ReferenceId =
                            salesReturn.Id,

                        InventoryBatchId =
                            null,

                        Notes =
                            $"Customer return {salesReturn.ReturnNumber}"
                    });

                quantityToRestore -=
                    unbatchedRestoreQuantity;
            }
        }

        if (quantityToRestore > 0)
        {
            throw new InvalidOperationException(
                $"Unable to trace {quantityToRestore:N3} returned units back to the original sale inventory movements.");
        }

        await IncreaseStockBalanceAsync(
            returnLine.ProductVariantId,
            originalSale.LocationId,
            returnLine.Quantity,
            cancellationToken);
    }

    private async Task IncreaseStockBalanceAsync(
        Guid productVariantId,
        Guid locationId,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var stockBalance =
            await _dbContext.StockBalances
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductVariantId ==
                            productVariantId &&
                        x.LocationId ==
                            locationId,
                    cancellationToken);

        if (stockBalance is null)
        {
            stockBalance =
                new StockBalance
                {
                    ProductVariantId =
                        productVariantId,

                    LocationId =
                        locationId,

                    QuantityOnHand =
                        quantity
                };

            _dbContext.StockBalances.Add(
                stockBalance);

            return;
        }

        stockBalance.QuantityOnHand +=
            quantity;
    }

    // ============================================================
    // HISTORICAL LINE FINANCIALS
    // ============================================================

    private static ReturnLineFinancials
        CalculateReturnLineFinancials(
            SaleLine original,
            decimal quantity,
            PreviousReturnTotals? previous,
            bool isFinalReturn)
    {
        if (original.Quantity <= 0)
        {
            throw new InvalidOperationException(
                "The original sale line has an invalid quantity.");
        }

        var previousLineTotal =
            previous?.LineTotal ?? 0m;

        var previousDiscount =
            previous?.DiscountAmount ?? 0m;

        var previousTax =
            previous?.TaxAmount ?? 0m;

        if (isFinalReturn)
        {
            return new ReturnLineFinancials
            {
                DiscountAmount =
                    RoundMoney(
                        original.DiscountAmount -
                        previousDiscount),

                TaxAmount =
                    RoundMoney(
                        original.TaxAmount -
                        previousTax),

                LineTotal =
                    RoundMoney(
                        original.LineTotal -
                        previousLineTotal)
            };
        }

        var ratio =
            quantity /
            original.Quantity;

        return new ReturnLineFinancials
        {
            DiscountAmount =
                RoundMoney(
                    original.DiscountAmount *
                    ratio),

            TaxAmount =
                RoundMoney(
                    original.TaxAmount *
                    ratio),

            LineTotal =
                RoundMoney(
                    original.LineTotal *
                    ratio)
        };
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateCreateRequest(
        CreateSalesReturnRequest request)
    {
        if (request.SaleId ==
            Guid.Empty)
        {
            throw new InvalidOperationException(
                "Sale is required.");
        }

        if (request.ReturnDate ==
            default)
        {
            throw new InvalidOperationException(
                "Return date is required.");
        }

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one return line is required.");
        }

        if (request.ReturnRequestId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "ReturnRequestId is required.");
        }

        foreach (var line
                 in request.Lines)
        {
            if (line.SaleLineId ==
                Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid sale line is required.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Return quantity must be greater than zero.");
            }
        }
    }

    private static void ValidateRefundRequest(
        CreateSalesReturnRequest request,
        decimal maximumRefundableAmount)
    {
        var refundAmount =
            RoundMoney(
                request.RefundAmount);

        if (refundAmount < 0)
        {
            throw new InvalidOperationException(
                "Refund amount cannot be negative.");
        }

        if (refundAmount >
            maximumRefundableAmount)
        {
            throw new InvalidOperationException(
                $"Refund amount cannot exceed the refundable amount of {maximumRefundableAmount:N2}.");
        }

        if (refundAmount == 0)
            return;

        if (!request.RefundPaymentMethod.HasValue)
        {
            throw new InvalidOperationException(
                "A refund payment method is required when refunding money.");
        }

        if (RequiresRefundReference(
                request.RefundPaymentMethod.Value) &&
            string.IsNullOrWhiteSpace(
                request.RefundReferenceNumber))
        {
            throw new InvalidOperationException(
                "A refund reference is required for this refund method.");
        }
    }

    private static bool RequiresRefundReference(
        PaymentMethod paymentMethod)
    {
        return paymentMethod !=
               PaymentMethod.Cash;
    }

    private static void EnsureSaleCanBeReturned(
        SaleStatus status)
    {
        if (status ==
            SaleStatus.Draft)
        {
            throw new InvalidOperationException(
                "A draft sale cannot be returned.");
        }

        if (status ==
            SaleStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "A cancelled sale cannot be returned.");
        }

        if (status ==
            SaleStatus.Refunded)
        {
            throw new InvalidOperationException(
                "This sale has already been fully refunded.");
        }
    }

    // ============================================================
    // CACHED SALE STATE
    // ============================================================

    private static void ApplySaleStatus(
        Sale sale,
        decimal netPaidAmount,
        decimal dueAmount)
    {
        if (dueAmount <= 0)
        {
            sale.Status =
                SaleStatus.Completed;

            return;
        }

        sale.Status =
            netPaidAmount > 0
                ? SaleStatus.PartiallyPaid
                : SaleStatus.Credit;
    }

    // ============================================================
    // RETURN NUMBER
    // ============================================================


    private async Task<string> GenerateReturnNumberAsync(
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Return numbering requires an active database transaction.");
        }

        var documentDate =
            DateOnly.FromDateTime(DateTime.UtcNow);

        const string documentPrefix = "RET";

        var datePart =
            documentDate.ToString("yyyyMMdd");

        var numberPrefix =
            $"{documentPrefix}-{datePart}-";

        var lockKey =
            $"InventoryPOS:DocumentNumber:{documentPrefix}:{datePart}";

        // Serialize number allocation across all terminals.
        await _dbContext.Database
            .SqlQuery<int>(
                $"""
            SELECT 1 AS "Value"
            FROM (
                SELECT pg_advisory_xact_lock(
                    hashtextextended({lockKey}, 0)
                )
            ) AS return_number_lock
            """)
            .SingleAsync(cancellationToken);

        var counter =
            await _dbContext.DocumentNumberCounters
                .SingleOrDefaultAsync(
                    x =>
                        x.DocumentPrefix == documentPrefix &&
                        x.DocumentDate == documentDate,
                    cancellationToken);

        if (counter is null)
        {
            // Preserve historical numbering when introducing
            // counters to an existing database.
            var historicalNumbers =
                await _dbContext.SalesReturns
                    .AsNoTracking()
                    .Where(x =>
                        x.ReturnNumber.StartsWith(numberPrefix))
                    .Select(x => x.ReturnNumber)
                    .ToListAsync(cancellationToken);

            var lastNumber = 0;

            foreach (var returnNumber in historicalNumbers)
            {
                var suffix =
                    returnNumber.Substring(numberPrefix.Length);

                if (int.TryParse(suffix, out var parsed) &&
                    parsed > lastNumber)
                {
                    lastNumber = parsed;
                }
            }

            counter = new DocumentNumberCounter
            {
                DocumentPrefix = documentPrefix,
                DocumentDate = documentDate,
                LastNumber = lastNumber
            };

            _dbContext.DocumentNumberCounters.Add(counter);
        }

        if (counter.LastNumber == int.MaxValue)
        {
            throw new InvalidOperationException(
                "Return number range has been exhausted.");
        }

        counter.LastNumber++;

        // The counter increment participates in the
        // existing sales-return database transaction.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return $"{numberPrefix}{counter.LastNumber:0000}";
    }


    // ============================================================
    // HELPERS
    // ============================================================

    private static decimal RoundMoney(
        decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string? Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value.Trim();
    }

    private static string ComputeReturnRequestFingerprint(
    CreateSalesReturnRequest request)
    {
        var normalized = new
        {
            request.SaleId,
            ReturnDate = request.ReturnDate.ToUniversalTime().Ticks,
            Reason = Clean(request.Reason),
            Notes = Clean(request.Notes),

            Lines = request.Lines
                .OrderBy(x => x.SaleLineId)
                .Select(x => new
                {
                    x.SaleLineId,
                    x.Quantity,
                    x.IsRestockable,
                    Reason = Clean(x.Reason)
                })
                .ToArray(),

            request.RefundAmount,
            request.RefundPaymentMethod,

            RefundReferenceNumber =
                Clean(request.RefundReferenceNumber),

            RefundNotes =
                Clean(request.RefundNotes)
        };

        var json = JsonSerializer.Serialize(normalized);

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(bytes);
    }

    private sealed class PreviousReturnTotals
    {
        public decimal Quantity { get; set; }

        public decimal LineTotal { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }
    }

    private sealed class ReturnLineFinancials
    {
        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal LineTotal { get; set; }
    }
}