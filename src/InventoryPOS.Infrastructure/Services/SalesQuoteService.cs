using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class SalesQuoteService : ISalesQuoteService
{
    private readonly InventoryPosDbContext _dbContext;

    public SalesQuoteService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<SalesQuoteResultDto> CreateAsync(
        CreateSalesQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // -----------------------------------------------------
        // Basic validation
        // -----------------------------------------------------

        if (request.LocationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A location is required.");
        }

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one quotation line is required.");
        }

        if (request.DiscountAmount < 0)
        {
            throw new InvalidOperationException(
                "Quote discount cannot be negative.");
        }

        if (request.TaxAmount < 0)
        {
            throw new InvalidOperationException(
                "Quote tax cannot be negative.");
        }

        // -----------------------------------------------------
        // Normalize dates
        // -----------------------------------------------------

        var quoteDate =
            request.QuoteDate == default
                ? DateTime.UtcNow
                : EnsureUtc(request.QuoteDate);

        DateTime? validUntilDate =
            request.ValidUntilDate.HasValue
                ? EnsureUtc(
                    request.ValidUntilDate.Value)
                : null;

        /*
         * ValidUntilDate represents a business/calendar date.
         *
         * Compare Date only so a quotation created later in the
         * day can still be valid through the current date.
         */
        if (validUntilDate.HasValue &&
            validUntilDate.Value.Date <
            quoteDate.Date)
        {
            throw new InvalidOperationException(
                "Valid-until date cannot be before the quote date.");
        }

        // -----------------------------------------------------
        // Duplicate product validation
        // -----------------------------------------------------

        if (request.Lines
            .GroupBy(x => x.ProductVariantId)
            .Any(x => x.Count() > 1))
        {
            throw new InvalidOperationException(
                "The same product cannot appear more than once.");
        }

        // -----------------------------------------------------
        // Line validation
        // -----------------------------------------------------

        foreach (var line in request.Lines)
        {
            if (line.ProductVariantId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Every quotation line requires a product.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Quantity must be greater than zero.");
            }

            if (line.UnitPrice < 0)
            {
                throw new InvalidOperationException(
                    "Unit price cannot be negative.");
            }

            if (line.DiscountAmount < 0)
            {
                throw new InvalidOperationException(
                    "Line discount cannot be negative.");
            }

            if (line.TaxAmount < 0)
            {
                throw new InvalidOperationException(
                    "Line tax cannot be negative.");
            }

            if (line.WarrantyMonths < 0)
            {
                throw new InvalidOperationException(
                    "Warranty months cannot be negative.");
            }

            var gross =
                line.Quantity *
                line.UnitPrice;

            if (line.DiscountAmount > gross)
            {
                throw new InvalidOperationException(
                    "Line discount cannot exceed the line value.");
            }
        }

        // -----------------------------------------------------
        // Validate location
        // -----------------------------------------------------

        var locationExists =
            await _dbContext.Locations
                .AnyAsync(
                    x =>
                        x.Id == request.LocationId &&
                        x.IsActive,
                    cancellationToken);

        if (!locationExists)
        {
            throw new InvalidOperationException(
                "The selected location is not available.");
        }

        // -----------------------------------------------------
        // Validate customer
        // -----------------------------------------------------

        if (request.CustomerId.HasValue)
        {
            var customerExists =
                await _dbContext.Customers
                    .AnyAsync(
                        x =>
                            x.Id ==
                                request.CustomerId.Value &&
                            x.IsActive,
                        cancellationToken);

            if (!customerExists)
            {
                throw new InvalidOperationException(
                    "The selected customer is not available.");
            }
        }

        // -----------------------------------------------------
        // Validate product variants
        // -----------------------------------------------------

        var variantIds =
            request.Lines
                .Select(x => x.ProductVariantId)
                .Distinct()
                .ToList();

        var validVariantCount =
            await _dbContext.ProductVariants
                .CountAsync(
                    x =>
                        variantIds.Contains(x.Id) &&
                        x.IsActive &&
                        x.Product != null &&
                        x.Product.IsActive,
                    cancellationToken);

        if (validVariantCount != variantIds.Count)
        {
            throw new InvalidOperationException(
                "One or more selected products are not available.");
        }

        // -----------------------------------------------------
        // Create quote header
        // -----------------------------------------------------

        var quote =
            new SalesQuote
            {
                QuoteNumber =
                    await GenerateQuoteNumberAsync(
                        quoteDate,
                        cancellationToken),

                LocationId =
                    request.LocationId,

                CustomerId =
                    request.CustomerId,

                QuoteDate =
                    quoteDate,

                ValidUntilDate =
                    validUntilDate,

                Status =
                    SalesQuoteStatus.Draft,

                DiscountAmount =
                    request.DiscountAmount,

                TaxAmount =
                    request.TaxAmount,

                Notes =
                    Normalize(
                        request.Notes),

                Terms =
                    Normalize(
                        request.Terms)
            };

        // -----------------------------------------------------
        // Create quote lines / calculate totals
        // -----------------------------------------------------

        decimal subtotal = 0m;
        decimal lineDiscounts = 0m;
        decimal lineTaxes = 0m;

        foreach (var requestLine in request.Lines)
        {
            var gross =
                requestLine.Quantity *
                requestLine.UnitPrice;

            var lineTotal =
                gross -
                requestLine.DiscountAmount +
                requestLine.TaxAmount;

            subtotal +=
                gross;

            lineDiscounts +=
                requestLine.DiscountAmount;

            lineTaxes +=
                requestLine.TaxAmount;

            quote.Lines.Add(
                new SalesQuoteLine
                {
                    ProductVariantId =
                        requestLine.ProductVariantId,

                    Quantity =
                        requestLine.Quantity,

                    UnitPrice =
                        requestLine.UnitPrice,

                    DiscountAmount =
                        requestLine.DiscountAmount,

                    TaxAmount =
                        requestLine.TaxAmount,

                    LineTotal =
                        lineTotal,

                    WarrantyMonths =
                        requestLine.WarrantyMonths,

                    Description =
                        Normalize(
                            requestLine.Description)
                });
        }

        // -----------------------------------------------------
        // Header totals
        // -----------------------------------------------------

        quote.Subtotal =
            subtotal;

        var totalDiscount =
            lineDiscounts +
            quote.DiscountAmount;

        var totalTax =
            lineTaxes +
            quote.TaxAmount;

        quote.TotalAmount =
            subtotal -
            totalDiscount +
            totalTax;

        if (quote.TotalAmount < 0)
        {
            throw new InvalidOperationException(
                "Quotation total cannot be negative.");
        }

        // -----------------------------------------------------
        // Persist
        // -----------------------------------------------------

        _dbContext.SalesQuotes.Add(
            quote);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        // -----------------------------------------------------
        // Result
        // -----------------------------------------------------

        return new SalesQuoteResultDto
        {
            SalesQuoteId =
                quote.Id,

            QuoteNumber =
                quote.QuoteNumber,

            LineCount =
                quote.Lines.Count,

            TotalQuantity =
                quote.Lines.Sum(
                    x => x.Quantity),

            TotalAmount =
                quote.TotalAmount
        };
    }

    // =========================================================
    // HISTORY
    // =========================================================

    public async Task<
        IReadOnlyList<SalesQuoteHistoryDto>>
        GetHistoryAsync(
            string? search = null,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.SalesQuotes
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(
                search))
        {
            var term =
                search.Trim();

            query =
                query.Where(
                    x =>
                        x.QuoteNumber.Contains(term) ||
                        (
                            x.Customer != null &&
                            x.Customer.Name.Contains(term)
                        ));
        }

        return await query
            .OrderByDescending(
                x => x.QuoteDate)
            .ThenByDescending(
                x => x.CreatedAtUtc)
            .Select(
                x =>
                    new SalesQuoteHistoryDto
                    {
                        Id =
                            x.Id,

                        QuoteNumber =
                            x.QuoteNumber,

                        QuoteDate =
                            x.QuoteDate,

                        ValidUntilDate =
                            x.ValidUntilDate,

                        CustomerName =
                            x.Customer != null
                                ? x.Customer.Name
                                : "Walk-in Customer",

                        LocationName =
                            x.Location != null
                                ? x.Location.Name
                                : string.Empty,

                        Status =
                            x.Status,

                        LineCount =
                            x.Lines.Count,

                        TotalQuantity =
                            x.Lines.Sum(
                                line =>
                                    line.Quantity),

                        TotalAmount =
                            x.TotalAmount,

                        SaleId =
                            x.SaleId
                    })
            .ToListAsync(
                cancellationToken);
    }

    // =========================================================
    // DETAILS
    // =========================================================

    public async Task<SalesQuoteDetailsDto?>
        GetByIdAsync(
            Guid salesQuoteId,
            CancellationToken cancellationToken = default)
    {
        if (salesQuoteId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.SalesQuotes
            .AsNoTracking()
            .Where(
                x =>
                    x.Id == salesQuoteId)
            .Select(
                x =>
                    new SalesQuoteDetailsDto
                    {
                        Id =
                            x.Id,

                        QuoteNumber =
                            x.QuoteNumber,

                        QuoteDate =
                            x.QuoteDate,

                        ValidUntilDate =
                            x.ValidUntilDate,

                        Status =
                            x.Status,

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

                        CustomerPhone =
                            x.Customer != null
                                ? x.Customer.Phone
                                : null,

                        CustomerEmail =
                            x.Customer != null
                                ? x.Customer.Email
                                : null,

                        Subtotal =
                            x.Subtotal,

                        DiscountAmount =
                            x.DiscountAmount,

                        TaxAmount =
                            x.TaxAmount,

                        TotalAmount =
                            x.TotalAmount,

                        Notes =
                            x.Notes,

                        Terms =
                            x.Terms,

                        SaleId =
                            x.SaleId,

                        InvoiceNumber =
                            x.Sale != null
                                ? x.Sale.InvoiceNumber
                                : null,

                        Lines =
                            x.Lines
                                .OrderBy(
                                    line =>
                                        line.CreatedAtUtc)
                                .Select(
                                    line =>
                                        new SalesQuoteDetailsLineDto
                                        {
                                            Id =
                                                line.Id,

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

                                            UnitSymbol =
                                                line.ProductVariant != null &&
                                                line.ProductVariant.Product != null &&
                                                line.ProductVariant.Product.Unit != null
                                                    ? line.ProductVariant.Product.Unit.Symbol
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

                                            WarrantyMonths =
                                                line.WarrantyMonths,

                                            Description =
                                                line.Description
                                        })
                                .ToList()
                    })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    // =========================================================
    // QUOTE NUMBER
    // =========================================================

    private async Task<string>
        GenerateQuoteNumberAsync(
            DateTime quoteDate,
            CancellationToken cancellationToken)
    {
        var date =
            quoteDate == default
                ? DateTime.UtcNow
                : EnsureUtc(quoteDate);

        var prefix =
            $"QT-{date:yyyyMMdd}-";

        var count =
            await _dbContext.SalesQuotes
                .CountAsync(
                    x =>
                        x.QuoteNumber
                            .StartsWith(prefix),
                    cancellationToken);

        return
            $"{prefix}{count + 1:000}";
    }

    public async Task IssueAsync(
    Guid salesQuoteId,
    CancellationToken cancellationToken = default)
    {
        var quote =
            await _dbContext.SalesQuotes
                .SingleOrDefaultAsync(
                    x => x.Id == salesQuoteId,
                    cancellationToken);

        if (quote is null)
        {
            throw new InvalidOperationException(
                "The quotation could not be found.");
        }

        if (quote.Status !=
            SalesQuoteStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a Draft quotation can be issued.");
        }

        quote.Status =
            SalesQuoteStatus.Issued;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }


    public async Task AcceptAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default)
    {
        var quote =
            await _dbContext.SalesQuotes
                .SingleOrDefaultAsync(
                    x => x.Id == salesQuoteId,
                    cancellationToken);

        if (quote is null)
        {
            throw new InvalidOperationException(
                "The quotation could not be found.");
        }

        if (quote.Status !=
            SalesQuoteStatus.Issued)
        {
            throw new InvalidOperationException(
                "Only an Issued quotation can be accepted.");
        }

        if (quote.ValidUntilDate.HasValue &&
            quote.ValidUntilDate.Value.Date <
            DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException(
                "This quotation has expired and cannot be accepted.");
        }

        quote.Status =
            SalesQuoteStatus.Accepted;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }


    public async Task CancelAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default)
    {
        var quote =
            await _dbContext.SalesQuotes
                .SingleOrDefaultAsync(
                    x => x.Id == salesQuoteId,
                    cancellationToken);

        if (quote is null)
        {
            throw new InvalidOperationException(
                "The quotation could not be found.");
        }

        if (quote.Status !=
                SalesQuoteStatus.Draft &&
            quote.Status !=
                SalesQuoteStatus.Issued &&
            quote.Status !=
                SalesQuoteStatus.Accepted)
        {
            throw new InvalidOperationException(
                "This quotation cannot be cancelled " +
                "in its current status.");
        }

        quote.Status =
            SalesQuoteStatus.Cancelled;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }


    public async Task ExpireAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default)
    {
        var quote =
            await _dbContext.SalesQuotes
                .SingleOrDefaultAsync(
                    x => x.Id == salesQuoteId,
                    cancellationToken);

        if (quote is null)
        {
            throw new InvalidOperationException(
                "The quotation could not be found.");
        }

        if (quote.Status !=
            SalesQuoteStatus.Issued)
        {
            throw new InvalidOperationException(
                "Only an Issued quotation can be marked expired.");
        }

        if (!quote.ValidUntilDate.HasValue)
        {
            throw new InvalidOperationException(
                "This quotation does not have an expiration date.");
        }

        if (quote.ValidUntilDate.Value.Date >=
            DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException(
                "This quotation has not expired yet.");
        }

        quote.Status =
            SalesQuoteStatus.Expired;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value.Trim();
    }

    private static DateTime EnsureUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc),

            _ =>
                value
        };
    }
}