using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly InventoryPosDbContext _dbContext;

    public SaleService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
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
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new PosCustomerDto
            {
                Id = x.Id,
                CustomerCode = x.CustomerCode,
                Name = x.Name,
                CreditLimit = x.CreditLimit,

                OutstandingBalance =
                    x.Sales
                        .Where(s =>
                            s.Status != SaleStatus.Cancelled &&
                            s.Status != SaleStatus.Refunded)
                        .Sum(s => s.DueAmount)
            })
            .ToListAsync(cancellationToken);
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

    public async Task<SaleResultDto> CreateSaleAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
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

            decimal subtotal = 0;
            decimal lineDiscountTotal = 0;
            decimal lineTaxTotal = 0;

            foreach (var line in request.Lines)
            {
                var gross =
                    line.Quantity * line.UnitPrice;

                if (line.DiscountAmount > gross)
                {
                    throw new InvalidOperationException(
                        "A line discount cannot exceed the line value.");
                }

                subtotal += gross;

                lineDiscountTotal +=
                    line.DiscountAmount;

                lineTaxTotal +=
                    line.TaxAmount;
            }

            var totalDiscount =
                lineDiscountTotal +
                request.OrderDiscountAmount;

            if (totalDiscount > subtotal)
            {
                throw new InvalidOperationException(
                    "The total discount cannot exceed the subtotal.");
            }

            var totalTax =
                lineTaxTotal +
                request.TaxAmount;

            var totalAmount =
                subtotal -
                totalDiscount +
                totalTax;

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
                    await _dbContext.Sales
                        .Where(x =>
                            x.CustomerId == customer.Id &&
                            x.Status != SaleStatus.Cancelled &&
                            x.Status != SaleStatus.Refunded)
                        .SumAsync(
                            x => (decimal?)x.DueAmount,
                            cancellationToken)
                    ?? 0m;

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

                var lineTotal =
                    requestLine.Quantity *
                    requestLine.UnitPrice -
                    requestLine.DiscountAmount +
                    requestLine.TaxAmount;

                var saleLine = new SaleLine
                {
                    Sale = sale,

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
                        lineTotal
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

            await transaction.CommitAsync(
                cancellationToken);

            return new SaleResultDto
            {
                SaleId =
                    sale.Id,

                InvoiceNumber =
                    sale.InvoiceNumber,

                TotalAmount =
                    sale.TotalAmount,

                PaidAmount =
                    sale.PaidAmount,

                DueAmount =
                    sale.DueAmount,

                Status =
                    sale.Status.ToString()
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
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

    private async Task<string>
        GenerateInvoiceNumberAsync(
            CancellationToken cancellationToken)
    {
        var today =
            DateTime.UtcNow.ToString(
                "yyyyMMdd");

        var prefix =
            $"INV-{today}-";

        var count =
            await _dbContext.Sales
                .CountAsync(
                    x =>
                        x.InvoiceNumber.StartsWith(
                            prefix),
                    cancellationToken);

        return $"{prefix}{count + 1:0000}";
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

            if (line.DiscountAmount < 0 ||
                line.TaxAmount < 0)
            {
                throw new InvalidOperationException(
                    "Discount and tax cannot be negative.");
            }
        }

        if (request.OrderDiscountAmount < 0 ||
            request.TaxAmount < 0)
        {
            throw new InvalidOperationException(
                "Discount and tax cannot be negative.");
        }

        foreach (var payment in request.Payments)
        {
            if (payment.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "Payment amount must be greater than zero.");
            }
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

                            TotalAmount =
                                x.TotalAmount,

                            PaidAmount =
                                x.PaidAmount,

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
                                        line.LineTotal
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
}