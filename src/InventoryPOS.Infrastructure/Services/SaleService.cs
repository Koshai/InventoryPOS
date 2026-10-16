using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Application.Pricing;
using InventoryPOS.Application.Validation;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryPOS.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly InventoryPosDbContext _dbContext;
    private readonly IPricingCalculator _pricingCalculator;
    private readonly ISaleFinancialService _financialService;
    private readonly ILicenseAccessGuard _licenseAccessGuard;

    public SaleService(
    InventoryPosDbContext dbContext,
    IPricingCalculator pricingCalculator,
    ISaleFinancialService financialService,
    ILicenseAccessGuard licenseAccessGuard)
    {
        _dbContext = dbContext;
        _pricingCalculator = pricingCalculator;
        _financialService = financialService;
        _licenseAccessGuard = licenseAccessGuard;
    }

    public async Task<IReadOnlyList<PosProductDto>>
        GetProductsAsync(
            Guid locationId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockBalances
            .AsNoTracking()
            .Where(x =>
                x.LocationId == locationId &&
                x.ProductVariant != null &&
                x.ProductVariant.IsActive &&
                x.ProductVariant.Product != null &&
                x.ProductVariant.Product.IsActive)
            .OrderBy(x =>
                x.ProductVariant!.Product!.Name)
            .Select(x => new PosProductDto
            {
                ProductVariantId =
                    x.ProductVariantId,

                SKU =
                    x.ProductVariant!.SKU,

                ProductName =
                    x.ProductVariant.Product!.Name,

                UnitSymbol =
                    x.ProductVariant.Product.Unit != null
                        ? x.ProductVariant.Product.Unit.Symbol
                        : string.Empty,

                SellingPrice =
                    x.ProductVariant.SellingPrice,

                QuantityAvailable =
                    x.QuantityOnHand -
                    x.QuantityReserved
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PosCustomerDto>>
    GetCustomersAsync(
        CancellationToken cancellationToken = default)
    {
        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new PosCustomerDto
            {
                Id = x.Id,
                CustomerCode = x.CustomerCode,
                Name = x.Name,
                CreditLimit = x.CreditLimit
            })
            .ToListAsync(cancellationToken);

        if (customers.Count == 0)
            return customers;

        var customerIds = customers
            .Select(x => x.Id)
            .ToList();

        var saleCustomerPairs = await _dbContext.Sales
            .AsNoTracking()
            .Where(x =>
                x.CustomerId.HasValue &&
                customerIds.Contains(x.CustomerId.Value) &&
                x.Status != SaleStatus.Cancelled &&
                x.Status != SaleStatus.Refunded)
            .Select(x => new
            {
                x.Id,
                CustomerId = x.CustomerId!.Value
            })
            .ToListAsync(cancellationToken);

        if (saleCustomerPairs.Count == 0)
            return customers;

        var states = await _financialService.GetStatesAsync(
            saleCustomerPairs.Select(x => x.Id),
            cancellationToken);

        var outstandingByCustomer = saleCustomerPairs
            .GroupBy(x => x.CustomerId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(x => states[x.Id].DueAmount));

        foreach (var customer in customers)
        {
            customer.OutstandingBalance =
                outstandingByCustomer.TryGetValue(
                    customer.Id,
                    out var outstanding)
                    ? outstanding
                    : 0m;
        }

        return customers;
    }

    public async Task<IReadOnlyList<LocationLookupDto>>
        GetLocationsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LocationLookupDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    // Read-only reconciliation: never allocates an invoice or deducts stock.
    // Fingerprint uses the exact same canonicalization as CreateSaleAsync.
    public async Task<SaleResultDto?> FindCommittedCheckoutAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CheckoutRequestId == Guid.Empty)
            throw new InvalidOperationException("A checkout request identifier is required.");

        var fingerprint = ComputeCheckoutFingerprint(request);
        var existingSale = await _dbContext.Sales
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CheckoutRequestId == request.CheckoutRequestId,
                cancellationToken);

        if (existingSale is null)
            return null;

        if (!string.Equals(existingSale.CheckoutRequestFingerprint,
                fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Checkout recovery conflict: the stored transaction does not match the local request. " +
                "Do not retry or discard the recovery journal until this is investigated.");

        return ToSaleResult(existingSale);
    }

    public async Task<SaleResultDto> CreateSaleAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (request.CheckoutRequestId == Guid.Empty)
            throw new InvalidOperationException(
                "A checkout request identifier is required.");

        var fingerprint = ComputeCheckoutFingerprint(request);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            // Transaction-scoped PostgreSQL advisory lock serializes requests
            // carrying the same checkout ID, including across workstations.
            var checkoutKey = request.CheckoutRequestId.ToString("D");
            await _dbContext.Database.SqlQuery<int>(
                $"""
                SELECT 1 AS "Value"
                FROM (
                    SELECT pg_advisory_xact_lock(
                        hashtextextended({checkoutKey}, 0)
                    )
                ) AS checkout_lock
                """)
                .SingleAsync(cancellationToken);

            // Read only after acquiring the lock: a competing transaction
            // may have committed while we waited.
            var existingSale = await _dbContext.Sales
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.CheckoutRequestId == request.CheckoutRequestId,
                    cancellationToken);

            if (existingSale is not null)
            {
                if (!string.Equals(
                    existingSale.CheckoutRequestFingerprint,
                    fingerprint,
                    StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "This checkout identifier was already used for a different transaction.");

                await transaction.CommitAsync(cancellationToken);
                return ToSaleResult(existingSale);
            }

            // A previously committed checkout may be recovered
            // regardless of license status.
            //
            // A genuinely new sale requires a usable license.
            _licenseAccessGuard.EnsureCanWrite();

            // Lock the accepted quotation before creating a sale.
            // This lock is held until the sale and quote are committed together.
            SalesQuote? sourceQuote = null;

            if (request.SalesQuoteId.HasValue)
            {
                var quoteId = request.SalesQuoteId.Value;

                sourceQuote = await _dbContext.SalesQuotes
                    .FromSqlInterpolated(
                        $"SELECT * FROM \"SalesQuotes\" WHERE \"Id\" = {quoteId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);

                if (sourceQuote is null)
                    throw new InvalidOperationException(
                        "The source quotation was not found.");

                if (sourceQuote.Status != SalesQuoteStatus.Accepted ||
                    sourceQuote.SaleId.HasValue)
                    throw new InvalidOperationException(
                        "This quotation has already been converted or is no longer accepted.");

                if (sourceQuote.ValidUntilDate.HasValue &&
                    sourceQuote.ValidUntilDate.Value.Date < DateTime.UtcNow.Date)
                    throw new InvalidOperationException(
                        "This quotation has expired.");

                if (sourceQuote.LocationId != request.LocationId ||
                    sourceQuote.CustomerId != request.CustomerId)
                    throw new InvalidOperationException(
                        "The sale's customer or location differs from the accepted quotation.");

                await _dbContext.Entry(sourceQuote)
                    .Collection(x => x.Lines)
                    .LoadAsync(cancellationToken);

                if (sourceQuote.Lines.Count != request.Lines.Count)
                    throw new InvalidOperationException(
                        "The sale must contain exactly the quoted products.");

                foreach (var quotedLine in sourceQuote.Lines)
                {
                    var matchingLines = request.Lines
                        .Where(x => x.ProductVariantId == quotedLine.ProductVariantId)
                        .ToList();

                    if (matchingLines.Count != 1 ||
                        matchingLines[0].Quantity != quotedLine.Quantity ||
                        matchingLines[0].UnitPrice != quotedLine.UnitPrice ||
                        matchingLines[0].DiscountAmount != quotedLine.DiscountAmount ||
                        matchingLines[0].TaxAmount != quotedLine.TaxAmount)
                        throw new InvalidOperationException(
                            "The sale's products, quantities or prices differ from the accepted quotation.");
                }

                if (sourceQuote.PricingVersion == 2)
                {
                    if (!request.UsePricingCalculator)
                        throw new InvalidOperationException(
                            "This quotation requires calculator-based pricing.");

                    if (request.OrderDiscountType != sourceQuote.OrderDiscountType ||
                        request.OrderDiscountValue != sourceQuote.OrderDiscountValue ||
                        request.OrderDiscountAmount != sourceQuote.OrderDiscountAmount ||
                        request.SalesTaxRate != sourceQuote.SalesTaxRate ||
                        request.TransportAmount != sourceQuote.TransportAmount ||
                        request.TaxAmount != sourceQuote.TaxAmount)
                    {
                        throw new InvalidOperationException(
                            "The sale's pricing terms differ from the accepted quotation.");
                    }

                    foreach (var quotedLine in sourceQuote.Lines)
                    {
                        var saleLine = request.Lines.Single(x =>
                            x.ProductVariantId == quotedLine.ProductVariantId);

                        if (saleLine.DiscountType != quotedLine.DiscountType ||
                            saleLine.DiscountValue != quotedLine.DiscountValue ||
                            saleLine.TaxRate != quotedLine.TaxRate)
                        {
                            throw new InvalidOperationException(
                                "The sale's line pricing terms differ from the accepted quotation.");
                        }
                    }
                }
                else if (sourceQuote.PricingVersion == 1)
                {
                    if (request.UsePricingCalculator)
                        throw new InvalidOperationException(
                            "Legacy quotations require legacy pricing.");

                    if (request.OrderDiscountAmount != sourceQuote.DiscountAmount ||
                        request.TaxAmount != sourceQuote.TaxAmount)
                    {
                        throw new InvalidOperationException(
                            "The sale's discounts or taxes differ from the legacy quotation.");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unsupported quotation pricing version: {sourceQuote.PricingVersion}.");
                }
            }

            var locationExists =
                await _dbContext.Locations.AnyAsync(
                    x =>
                        x.Id == request.LocationId &&
                        x.IsActive,
                    cancellationToken);

            if (!locationExists)
            {
                throw new InvalidOperationException(
                    "The selected location is invalid or inactive.");
            }

            Customer? customer = null;

            if (request.CustomerId.HasValue)
            {
                customer =
                    await _dbContext.Customers
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id == request.CustomerId.Value &&
                                x.IsActive,
                            cancellationToken);

                if (customer is null)
                {
                    throw new InvalidOperationException(
                        "The selected customer is invalid or inactive.");
                }
            }

            var duplicateProduct =
                request.Lines
                    .GroupBy(x => x.ProductVariantId)
                    .Any(x => x.Count() > 1);

            if (duplicateProduct)
            {
                throw new InvalidOperationException(
                    "The same product cannot appear more than once in a sale.");
            }

            // Explicit request mode: legacy monetary snapshots or new calculator inputs.
            var calculatorMode = request.UsePricingCalculator;
            PricingCalculationResult? pricing = null;
            decimal subtotal;
            decimal lineDiscountTotal;
            decimal totalDiscount;
            decimal totalTax;
            decimal totalAmount;

            if (calculatorMode)
            {
                pricing = _pricingCalculator.Calculate(new PricingCalculationRequest
                {
                    Lines = request.Lines.Select(line => new PricingLineRequest
                    {
                        ProductVariantId = line.ProductVariantId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        DiscountType = (PricingAdjustmentType)line.DiscountType,
                        DiscountValue = line.DiscountValue,
                        TaxRate = line.TaxRate
                    }).ToList(),
                    OrderDiscountType = (PricingAdjustmentType)request.OrderDiscountType,
                    OrderDiscountValue = request.OrderDiscountValue,
                    TaxRate = request.SalesTaxRate,
                    TransportAmount = request.TransportAmount
                });

                if (request.OrderDiscountAmount != pricing.OrderDiscountAmount ||
                    request.TaxAmount != pricing.TaxAmount)
                    throw new InvalidOperationException(
                        "Submitted order discount or tax differs from the calculated amount.");

                foreach (var line in request.Lines)
                {
                    var calculated = pricing.Lines.Single(x =>
                        x.ProductVariantId == line.ProductVariantId);
                    if (line.DiscountAmount != calculated.DiscountAmount ||
                        line.TaxAmount != calculated.TaxAmount)
                        throw new InvalidOperationException(
                            "Submitted line discount or tax differs from the calculated amount.");
                }

                subtotal = pricing.GrossAmount;
                lineDiscountTotal = pricing.LineDiscountAmount;
                totalDiscount = pricing.DiscountAmount;
                totalTax = pricing.TaxAmount;
                totalAmount = pricing.TotalAmount;
                if (sourceQuote is not null &&
    sourceQuote.PricingVersion == 2)
                {
                    if (pricing.GrossAmount != sourceQuote.Subtotal ||
                        pricing.LineDiscountAmount != sourceQuote.LineDiscountAmount ||
                        pricing.OrderDiscountAmount != sourceQuote.OrderDiscountAmount ||
                        pricing.DiscountAmount != sourceQuote.DiscountAmount ||
                        pricing.TaxAmount != sourceQuote.TaxAmount ||
                        pricing.TransportAmount != sourceQuote.TransportAmount ||
                        pricing.TotalAmount != sourceQuote.TotalAmount)
                    {
                        throw new InvalidOperationException(
                            "The calculated sale does not match the accepted quotation's financial snapshot.");
                    }

                    foreach (var quotedLine in sourceQuote.Lines)
                    {
                        var calculatedLine = pricing.Lines.Single(x =>
                            x.ProductVariantId == quotedLine.ProductVariantId);

                        if (calculatedLine.DiscountAmount != quotedLine.DiscountAmount ||
                            calculatedLine.TaxAmount != quotedLine.TaxAmount ||
                            calculatedLine.LineTotal != quotedLine.LineTotal)
                        {
                            throw new InvalidOperationException(
                                "The calculated sale line does not match the accepted quotation.");
                        }
                    }
                }
            }
            else
            {
                // Preserve existing callers, including accepted quotation conversion.
                if (request.TransportAmount != 0m || request.SalesTaxRate != 0m ||
                    request.OrderDiscountValue != 0m ||
                    request.Lines.Any(x => x.DiscountValue != 0m || x.TaxRate != 0m))
                    throw new InvalidOperationException(
                        "Calculator pricing inputs require UsePricingCalculator=true.");

                subtotal = 0m;
                lineDiscountTotal = 0m;
                decimal lineTaxTotal = 0m;
                foreach (var line in request.Lines)
                {
                    var gross = line.Quantity * line.UnitPrice;
                    if (line.DiscountAmount > gross)
                        throw new InvalidOperationException(
                            "A line discount cannot exceed the line value.");
                    subtotal += gross;
                    lineDiscountTotal += line.DiscountAmount;
                    lineTaxTotal += line.TaxAmount;
                }
                totalDiscount = lineDiscountTotal + request.OrderDiscountAmount;
                if (totalDiscount > subtotal)
                    throw new InvalidOperationException(
                        "The total discount cannot exceed the subtotal.");
                totalTax = lineTaxTotal + request.TaxAmount;
                totalAmount = subtotal - totalDiscount + totalTax;
            }

            var paidAmount =
                request.Payments.Sum(x => x.Amount);

            if (paidAmount < 0)
            {
                throw new InvalidOperationException(
                    "Payment cannot be negative.");
            }

            if (paidAmount > totalAmount)
            {
                throw new InvalidOperationException(
                    "Payment cannot exceed the sale total.");
            }

            var dueAmount =
                totalAmount - paidAmount;

            if (dueAmount > 0 &&
                !request.CustomerId.HasValue)
            {
                throw new InvalidOperationException(
                    "A customer is required when a sale has an outstanding balance.");
            }

            if (dueAmount > 0)
            {
                if (customer is null)
                {
                    throw new InvalidOperationException(
                        "A customer is required when a sale has an outstanding balance.");
                }

                var existingOutstanding =
                    await GetCustomerOutstandingAsync(
                    customer.Id,
                    cancellationToken);

                var availableCredit =
                    customer.CreditLimit -
                    existingOutstanding;

                if (availableCredit < 0)
                {
                    availableCredit = 0;
                }

                if (dueAmount > availableCredit)
                {
                    throw new InvalidOperationException(
                        $"Credit limit exceeded. " +
                        $"Available credit: ৳{availableCredit:N2}. " +
                        $"Requested credit: ৳{dueAmount:N2}.");
                }
            }

            var status =
                DetermineStatus(
                    totalAmount,
                    paidAmount);

            var sale = new Sale
            {
                CheckoutRequestId = request.CheckoutRequestId,
                CheckoutRequestFingerprint = fingerprint,
                InvoiceNumber =
                    await GenerateInvoiceNumberAsync(
                        cancellationToken),

                SaleDate =
                    DateTime.UtcNow,

                LocationId =
                    request.LocationId,

                CustomerId =
                    request.CustomerId,

                Status =
                    status,

                Subtotal =
                    subtotal,

                DiscountAmount =
                    totalDiscount,

                LineDiscountAmount = lineDiscountTotal,
                OrderDiscountAmount = request.OrderDiscountAmount,
                TransportAmount = calculatorMode ? request.TransportAmount : 0m,
                SalesTaxRate = calculatorMode ? request.SalesTaxRate : 0m,

                TaxAmount =
                    totalTax,

                TotalAmount =
                    totalAmount,

                PaidAmount =
                    paidAmount,

                DueAmount =
                    dueAmount,

                Notes =
                    NormalizeOptional(
                        request.Notes)
            };

            _dbContext.Sales.Add(sale);

            foreach (var requestLine in request.Lines)
            {
                var productVariant =
                    await _dbContext.ProductVariants
                        .Include(x => x.Product)
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                requestLine.ProductVariantId,
                            cancellationToken);

                if (productVariant is null ||
                    !productVariant.IsActive ||
                    productVariant.Product is null ||
                    !productVariant.Product.IsActive)
                {
                    throw new InvalidOperationException(
                        "One of the selected products is invalid or inactive.");
                }

                var stockBalance =
                    await _dbContext.StockBalances
                        .FirstOrDefaultAsync(
                            x =>
                                x.ProductVariantId ==
                                    requestLine.ProductVariantId &&
                                x.LocationId ==
                                    request.LocationId,
                            cancellationToken);

                if (stockBalance is null)
                {
                    throw new InvalidOperationException(
                        $"No stock balance exists for {productVariant.SKU}.");
                }

                var available =
                    stockBalance.QuantityOnHand -
                    stockBalance.QuantityReserved;

                if (available < requestLine.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for {productVariant.SKU}. " +
                        $"Available: {available:N3}.");
                }

                var calculatedLine = pricing?.Lines.Single(x =>
                    x.ProductVariantId == requestLine.ProductVariantId);
                var lineTotal = calculatedLine?.LineTotal ??
                    (requestLine.Quantity * requestLine.UnitPrice -
                     requestLine.DiscountAmount + requestLine.TaxAmount);

                // Quoted warranties are historical snapshots. Ordinary sales
                // continue using the product's current default warranty.
                int? warrantyMonths = sourceQuote is null
                    ? productVariant.DefaultWarrantyMonths
                    : sourceQuote.Lines.Single(x =>
                        x.ProductVariantId == requestLine.ProductVariantId)
                        .WarrantyMonths;

                DateTime? warrantyStartDate = null;
                DateTime? warrantyEndDate = null;

                if (warrantyMonths.HasValue &&
                    warrantyMonths.Value > 0)
                {
                    warrantyStartDate = sale.SaleDate;

                    warrantyEndDate =
                        sale.SaleDate.AddMonths(
                            warrantyMonths.Value);
                }

                var saleLine = new SaleLine
                {
                    Sale = sale,

                    ProductVariantId =
                        requestLine.ProductVariantId,

                    Quantity =
                        requestLine.Quantity,

                    UnitPrice =
                        requestLine.UnitPrice,

                    DiscountAmount = requestLine.DiscountAmount,
                    TaxAmount = requestLine.TaxAmount,
                    DiscountType = calculatorMode ? requestLine.DiscountType : 1,
                    DiscountValue = calculatorMode ? requestLine.DiscountValue : 0m,
                    TaxRate = calculatorMode ? requestLine.TaxRate : 0m,

                    LineTotal =
                        lineTotal,

                    WarrantyMonths =
                        warrantyMonths,

                    WarrantyStartDate =
                        warrantyStartDate,

                    WarrantyEndDate =
                        warrantyEndDate
                };

                _dbContext.SaleLines.Add(
                    saleLine);

                await ConsumeStockAsync(
                    sale,
                    saleLine,
                    stockBalance,
                    requestLine.Quantity,
                    request.LocationId,
                    cancellationToken);
            }

            foreach (var payment in request.Payments)
            {
                _dbContext.SalePayments.Add(
                    new SalePayment
                    {
                        Sale = sale,

                        PaymentMethod =
                            payment.PaymentMethod,

                        Amount =
                            payment.Amount,

                        PaymentDate =
                            DateTime.UtcNow,

                        ReferenceNumber =
                            NormalizeOptional(
                                payment.ReferenceNumber)
                    });
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // Persist the quotation link in the SAME transaction as the sale,
            // payments, inventory batches and stock movements.
            if (sourceQuote is not null)
            {
                sourceQuote.SaleId = sale.Id;
                sourceQuote.Status = SalesQuoteStatus.ConvertedToSale;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);

            return ToSaleResult(sale);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static SaleResultDto ToSaleResult(Sale sale)
    {
        return new SaleResultDto
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            TotalAmount = sale.TotalAmount,
            PaidAmount = sale.PaidAmount,
            DueAmount = sale.DueAmount,
            Status = sale.Status.ToString()
        };
    }

    private static string ComputeCheckoutFingerprint(CreateSaleRequest request)
    {
        // Sort order-insensitive collections for stable retries.
        // The identifier is deliberately excluded from the payload.
        var snapshot = new
        {
            request.LocationId,
            request.CustomerId,
            request.SalesQuoteId,
            request.UsePricingCalculator,
            request.OrderDiscountType,
            request.OrderDiscountValue,
            request.OrderDiscountAmount,
            request.SalesTaxRate,
            request.TaxAmount,
            request.TransportAmount,
            Notes = NormalizeOptional(request.Notes),
            Lines = request.Lines
                .OrderBy(x => x.ProductVariantId)
                .Select(x => new
                {
                    x.ProductVariantId,
                    x.Quantity,
                    x.UnitPrice,
                    x.DiscountType,
                    x.DiscountValue,
                    x.DiscountAmount,
                    x.TaxRate,
                    x.TaxAmount
                })
                .ToArray(),
            Payments = request.Payments
                .Select(x => new
                {
                    x.PaymentMethod,
                    x.Amount,
                    ReferenceNumber = NormalizeOptional(x.ReferenceNumber)
                })
                .OrderBy(x => x.PaymentMethod)
                .ThenBy(x => x.Amount)
                .ThenBy(x => x.ReferenceNumber)
                .ToArray()
        };

        var json = JsonSerializer.Serialize(snapshot);
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private async Task ConsumeStockAsync(
        Sale sale,
        SaleLine saleLine,
        StockBalance stockBalance,
        decimal quantity,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var remaining =
            quantity;

        var batches =
            await _dbContext.InventoryBatches
                .Where(x =>
                    x.ProductVariantId ==
                        saleLine.ProductVariantId &&
                    x.LocationId ==
                        locationId &&
                    x.QuantityOnHand > 0)
                .OrderBy(x =>
                    x.ExpiryDate == null)
                .ThenBy(x =>
                    x.ExpiryDate)
                .ThenBy(x =>
                    x.CreatedAtUtc)
                .ToListAsync(
                    cancellationToken);

        foreach (var batch in batches)
        {
            if (remaining <= 0)
                break;

            var consumed =
                Math.Min(
                    remaining,
                    batch.QuantityOnHand);

            batch.QuantityOnHand -=
                consumed;

            remaining -=
                consumed;

            _dbContext.StockMovements.Add(
                new StockMovement
                {
                    ProductVariantId =
                        saleLine.ProductVariantId,

                    LocationId =
                        locationId,

                    InventoryBatchId =
                        batch.Id,

                    MovementType =
                        InventoryMovementType.Sale,

                    Quantity =
                        -consumed,

                    ReferenceType =
                        "Sale",

                    ReferenceId =
                        sale.Id,

                    Notes =
                        $"Sale {sale.InvoiceNumber}"
                });
        }

        /*
         * Some existing inventory may have been created before
         * batch tracking was introduced.
         *
         * Therefore StockBalance can contain valid stock that
         * does not yet exist in InventoryBatches.
         *
         * For now we allow that stock to be consumed with a
         * non-batch StockMovement.
         */

        if (remaining > 0)
        {
            var batchTrackedQuantity =
                batches.Sum(x => x.QuantityOnHand);

            var balanceAfterSale =
                stockBalance.QuantityOnHand -
                quantity;

            if (balanceAfterSale < 0)
            {
                throw new InvalidOperationException(
                    "Insufficient inventory.");
            }

            _dbContext.StockMovements.Add(
                new StockMovement
                {
                    ProductVariantId =
                        saleLine.ProductVariantId,

                    LocationId =
                        locationId,

                    InventoryBatchId =
                        null,

                    MovementType =
                        InventoryMovementType.Sale,

                    Quantity =
                        -remaining,

                    ReferenceType =
                        "Sale",

                    ReferenceId =
                        sale.Id,

                    Notes =
                        $"Sale {sale.InvoiceNumber} " +
                        "(unbatched stock)"
                });
        }

        stockBalance.QuantityOnHand -=
            quantity;
    }

    private static SaleStatus DetermineStatus(
        decimal total,
        decimal paid)
    {
        if (paid <= 0)
            return SaleStatus.Credit;

        if (paid < total)
            return SaleStatus.PartiallyPaid;

        return SaleStatus.Completed;
    }

    private async Task<string> GenerateInvoiceNumberAsync(
        CancellationToken cancellationToken)
    {
        // Called inside CreateSaleAsync's existing PostgreSQL transaction.
        // The transaction-scoped advisory lock serializes all invoice
        // allocations for this UTC date, across POS workstations.
        if (_dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "Invoice numbering requires an active database transaction.");

        var documentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        const string documentPrefix = "INV";
        var datePart = documentDate.ToString("yyyyMMdd");
        var invoicePrefix = $"{documentPrefix}-{datePart}-";
        var lockKey = $"InventoryPOS:DocumentNumber:{documentPrefix}:{datePart}";

        await _dbContext.Database.SqlQuery<int>(
            $"""
            SELECT 1 AS "Value"
            FROM (
                SELECT pg_advisory_xact_lock(
                    hashtextextended({lockKey}, 0)
                )
            ) AS invoice_number_lock
            """)
            .SingleAsync(cancellationToken);

        var counter = await _dbContext.DocumentNumberCounters
            .SingleOrDefaultAsync(
                x => x.DocumentPrefix == documentPrefix &&
                     x.DocumentDate == documentDate,
                cancellationToken);

        if (counter is null)
        {
            // Seed from the largest existing suffix, NOT a row count.
            // This is necessary when introducing counters to an existing DB.
            var historicalNumbers = await _dbContext.Sales
                .AsNoTracking()
                .Where(x => x.InvoiceNumber.StartsWith(invoicePrefix))
                .Select(x => x.InvoiceNumber)
                .ToListAsync(cancellationToken);

            var lastNumber = 0;
            foreach (var invoiceNumber in historicalNumbers)
            {
                var suffix = invoiceNumber.Substring(invoicePrefix.Length);
                if (int.TryParse(suffix, out var parsed) &&
                    parsed > lastNumber)
                    lastNumber = parsed;
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
            throw new InvalidOperationException(
                "Invoice number range has been exhausted.");

        counter.LastNumber++;

        // Save the counter inside the sale transaction. If checkout fails,
        // both the counter increment and sale are rolled back together.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return $"{invoicePrefix}{counter.LastNumber:0000}";
    }

    private static void ValidateRequest(
        CreateSaleRequest request)
    {
        if (request.LocationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Location is required.");
        }

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "A sale must contain at least one product.");
        }

        foreach (var line in request.Lines)
        {
            if (line.ProductVariantId ==
                Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A product is required.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Sale quantity must be greater than zero.");
            }

            if (line.UnitPrice <= 0)
            {
                throw new InvalidOperationException(
                    "Unit price must be greater than zero.");
            }

            if (line.DiscountValue < 0 || line.TaxRate < 0 ||
                line.TaxRate > 100 ||
                !Enum.IsDefined(typeof(PricingAdjustmentType), line.DiscountType))
                throw new InvalidOperationException("Invalid line pricing inputs.");

            if (line.DiscountAmount < 0 ||
                line.TaxAmount < 0)
            {
                throw new InvalidOperationException(
                    "Discount and tax cannot be negative.");
            }
        }

        if (request.OrderDiscountAmount < 0 ||
            request.TaxAmount < 0 || request.TransportAmount < 0 ||
            request.OrderDiscountValue < 0 ||
            request.SalesTaxRate < 0 || request.SalesTaxRate > 100)
        {
            throw new InvalidOperationException(
                "Discount and tax cannot be negative.");
        }

        if (!Enum.IsDefined(typeof(PricingAdjustmentType), request.OrderDiscountType))
            throw new InvalidOperationException("Invalid order discount type.");

        foreach (var payment in request.Payments)
        {
            PaymentValidator.Validate(
                payment.PaymentMethod,
                payment.Amount,
                payment.ReferenceNumber);
        }
    }

    public async Task<SalesHistoryResultDto> GetSalesHistoryAsync(
    SalesHistoryQuery request,
    CancellationToken cancellationToken = default)
    {
        var pageNumber =
            request.PageNumber < 1
                ? 1
                : request.PageNumber;

        var pageSize =
            request.PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => request.PageSize
            };

        var query =
            _dbContext.Sales
                .AsNoTracking()
                .AsQueryable();

        // ---------------------------------------------------------
        // Search
        // ---------------------------------------------------------

        var searchText =
            request.SearchText?.Trim();

        if (!string.IsNullOrWhiteSpace(
                searchText))
        {
            query =
                query.Where(
                    x =>
                        x.InvoiceNumber.Contains(
                            searchText) ||

                        (x.Customer != null &&
                         (
                             x.Customer.Name.Contains(
                                 searchText) ||

                             x.Customer.CustomerCode.Contains(
                                 searchText)
                         )));
        }

        // ---------------------------------------------------------
        // Status
        // ---------------------------------------------------------

        if (request.Status.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.Status ==
                        request.Status.Value);
        }

        // ---------------------------------------------------------
        // Location
        // ---------------------------------------------------------

        if (request.LocationId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.LocationId ==
                        request.LocationId.Value);
        }

        // ---------------------------------------------------------
        // Date range
        // ---------------------------------------------------------

        if (request.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(
                request.FromDate.Value.Date,
                DateTimeKind.Utc);

            query =
                query.Where(
                    x => x.SaleDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var until = DateTime.SpecifyKind(
                request.ToDate.Value.Date.AddDays(1),
                DateTimeKind.Utc);

            query =
                query.Where(
                    x => x.SaleDate < until);
        }

        // ---------------------------------------------------------
        // Count before paging
        // ---------------------------------------------------------

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        // ---------------------------------------------------------
        // Page
        // ---------------------------------------------------------

        var items =
            await query
                .OrderByDescending(
                    x => x.SaleDate)
                .ThenByDescending(
                    x => x.InvoiceNumber)
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(
                    pageSize)
                .Select(
                    x =>
                        new SalesHistoryDto
                        {
                            SaleId =
                                x.Id,

                            InvoiceNumber =
                                x.InvoiceNumber,

                            SaleDate =
                                x.SaleDate,

                            CustomerName =
                                x.Customer != null
                                    ? x.Customer.Name
                                    : "Walk-in Customer",

                            LocationName =
                                x.Location != null
                                    ? x.Location.Name
                                    : string.Empty,

                            Subtotal = x.Subtotal,

                            LineDiscountAmount = x.LineDiscountAmount,

                            OrderDiscountAmount = x.OrderDiscountAmount,

                            DiscountAmount = x.DiscountAmount,

                            TaxAmount = x.TaxAmount,

                            SalesTaxRate = x.SalesTaxRate,

                            TransportAmount = x.TransportAmount,

                            TotalAmount = x.TotalAmount,

                            PaidAmount = x.PaidAmount,

                            DueAmount =
                                x.DueAmount,

                            Status =
                                x.Status
                        })
                .ToListAsync(
                    cancellationToken);

        return new SalesHistoryResultDto
        {
            Items =
                items,

            PageNumber =
                pageNumber,

            PageSize =
                pageSize,

            TotalCount =
                totalCount
        };
    }

    public async Task<SaleDetailsDto?> GetSaleDetailsAsync(
    Guid saleId,
    CancellationToken cancellationToken = default)
    {
        var sale =
            await _dbContext.Sales
                .AsNoTracking()
                .Where(x => x.Id == saleId)
                .Select(x => new SaleDetailsDto
                {
                    SaleId = x.Id,

                    InvoiceNumber = x.InvoiceNumber,

                    SaleDate = x.SaleDate,

                    Status = x.Status,

                    // ---------------------------------------------
                    // Location
                    // ---------------------------------------------

                    LocationId = x.LocationId,

                    LocationName =
                        x.Location != null
                            ? x.Location.Name
                            : string.Empty,

                    // ---------------------------------------------
                    // Customer
                    // ---------------------------------------------

                    CustomerId = x.CustomerId,

                    CustomerName =
                        x.Customer != null
                            ? x.Customer.Name
                            : "Walk-in Customer",

                    CustomerCode =
                        x.Customer != null
                            ? x.Customer.CustomerCode
                            : null,

                    CustomerPhone =
                        x.Customer != null
                            ? x.Customer.Phone
                            : null,

                    CustomerAddress =
                        x.Customer != null
                            ? x.Customer.Address
                            : null,

                    // ---------------------------------------------
                    // Totals
                    // ---------------------------------------------

                    Subtotal = x.Subtotal,

                    DiscountAmount =
                        x.DiscountAmount,

                    LineDiscountAmount = x.LineDiscountAmount,
                    OrderDiscountAmount = x.OrderDiscountAmount,
                    TransportAmount = x.TransportAmount,
                    SalesTaxRate = x.SalesTaxRate,

                    TaxAmount =
                        x.TaxAmount,

                    TotalAmount =
                        x.TotalAmount,

                    PaidAmount =
                        x.PaidAmount,

                    DueAmount =
                        x.DueAmount,

                    Notes =
                        x.Notes,

                    // ---------------------------------------------
                    // Lines
                    // ---------------------------------------------

                    Lines =
                        x.Lines
                            .OrderBy(line => line.CreatedAtUtc)
                            .Select(line =>
                                new SaleDetailsLineDto
                                {
                                    SaleLineId =
                                        line.Id,

                                    ProductVariantId =
                                        line.ProductVariantId,

                                    SKU =
                                        line.ProductVariant != null
                                            ? line.ProductVariant.SKU
                                            : string.Empty,

                                    ProductName =
                                        line.ProductVariant != null &&
                                        line.ProductVariant.Product != null
                                            ? line.ProductVariant.Product.Name
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

                                    WarrantyStartDate =
                                        line.WarrantyStartDate,

                                    WarrantyEndDate =
                                        line.WarrantyEndDate
                                })
                            .ToList(),

                    // ---------------------------------------------
                    // Payments
                    // ---------------------------------------------

                    Payments =
                        x.Payments
                            .OrderBy(payment =>
                                payment.PaymentDate)
                            .Select(payment =>
                                new SaleDetailsPaymentDto
                                {
                                    SalePaymentId =
                                        payment.Id,

                                    PaymentMethod =
                                        payment.PaymentMethod,

                                    Amount =
                                        payment.Amount,

                                    PaymentDate =
                                        payment.PaymentDate,

                                    ReferenceNumber =
                                        payment.ReferenceNumber
                                })
                            .ToList()
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        return sale;
    }



    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private async Task<decimal> GetCustomerOutstandingAsync(
    Guid customerId,
    CancellationToken cancellationToken)
    {
        var saleIds = await _dbContext.Sales
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status != SaleStatus.Cancelled &&
                x.Status != SaleStatus.Refunded)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (saleIds.Count == 0)
            return 0m;

        var states = await _financialService.GetStatesAsync(
            saleIds,
            cancellationToken);

        return states.Values.Sum(x => x.DueAmount);
    }
}
