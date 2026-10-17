using InventoryPOS.Application.Licensing;
using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class PurchaseReturnService
    : IPurchaseReturnService
{
    private readonly InventoryPosDbContext _dbContext;

    private readonly ILicenseAccessGuard _licenseAccessGuard;


    public PurchaseReturnService(
        InventoryPosDbContext dbContext,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _licenseAccessGuard = licenseAccessGuard ?? throw new ArgumentNullException(nameof(licenseAccessGuard));
        _dbContext = dbContext;
    }

    // =========================================================
    // GET RETURNABLE RECEIPT
    // =========================================================

    public async Task<PurchaseReturnableReceiptDto?>
        GetReturnableReceiptAsync(
            Guid goodsReceiptId,
            CancellationToken cancellationToken = default)
    {
        var receipt =
            await _dbContext.GoodsReceipts
                .AsNoTracking()
                .Where(x =>
                    x.Id == goodsReceiptId)
                .Select(x =>
                    new PurchaseReturnableReceiptDto
                    {
                        GoodsReceiptId =
                            x.Id,

                        ReceiptNumber =
                            x.ReceiptNumber,

                        ReceiptDate =
                            x.ReceivedAt,

                        SupplierId =
                            x.SupplierId,

                        SupplierName =
                            x.Supplier != null
                                ? x.Supplier.Name
                                : string.Empty,

                        LocationId =
                            x.LocationId,

                        LocationName =
                            x.Location != null
                                ? x.Location.Name
                                : string.Empty,

                        PurchaseOrderNumber =
                            x.PurchaseOrder != null
                                ? x.PurchaseOrder.OrderNumber
                                : string.Empty,

                        Lines =
                            x.Lines
                                .OrderBy(line =>
                                    line.ProductVariant != null &&
                                    line.ProductVariant.Product != null
                                        ? line.ProductVariant.Product.Name
                                        : string.Empty)
                                .Select(line =>
                                    new PurchaseReturnableLineDto
                                    {
                                        GoodsReceiptLineId =
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

                                        ReceivedQuantity =
                                            line.ReceivedQuantity,

                                        UnitCost =
                                            line.UnitCost,

                                        ExpiryDate =
                                            line.ExpiryDate,

                                        WarrantyMonths =
                                            line.WarrantyMonths
                                    })
                                .ToList()
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (receipt is null)
            return null;

        // -----------------------------------------------------
        // Resolve batch + previous completed returns
        // -----------------------------------------------------

        foreach (var line in receipt.Lines)
        {
            var batch =
                await _dbContext.InventoryBatches
                    .AsNoTracking()
                    .Where(x =>
                        x.GoodsReceiptLineId ==
                            line.GoodsReceiptLineId &&
                        x.ProductVariantId ==
                            line.ProductVariantId &&
                        x.LocationId ==
                            receipt.LocationId &&
                        x.SupplierId ==
                            receipt.SupplierId)
                    .Select(x => new
                    {
                        x.Id,
                        x.SupplierBatchNumber,
                        x.QuantityOnHand
                    })
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (batch is not null)
            {
                line.InventoryBatchId =
                    batch.Id;

                line.SupplierBatchNumber =
                    batch.SupplierBatchNumber;

                line.BatchQuantityOnHand =
                    batch.QuantityOnHand;
            }

            line.PreviouslyReturnedQuantity =
                await _dbContext.PurchaseReturnLines
                    .AsNoTracking()
                    .Where(x =>
                        x.GoodsReceiptLineId ==
                            line.GoodsReceiptLineId &&
                        x.PurchaseReturn != null &&
                        x.PurchaseReturn.Status ==
                            PurchaseReturnStatus.Completed)
                    .SumAsync(
                        x => (decimal?)x.Quantity,
                        cancellationToken)
                ?? 0;
        }

        return receipt;
    }

    // =========================================================
    // CREATE RETURN
    // =========================================================

    public async Task<PurchaseReturnResultDto>
        CreateReturnAsync(
            CreatePurchaseReturnRequest request,
            CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        ValidateRequest(request);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            // -------------------------------------------------
            // Load original receipt
            // -------------------------------------------------

            var receipt =
                await _dbContext.GoodsReceipts
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            request.GoodsReceiptId,
                        cancellationToken);

            if (receipt is null)
            {
                throw new InvalidOperationException(
                    "Goods receipt could not be found.");
            }

            // -------------------------------------------------
            // Prevent duplicate receipt lines in request
            // -------------------------------------------------

            var duplicateLine =
                request.Lines
                    .GroupBy(x =>
                        x.GoodsReceiptLineId)
                    .Any(x =>
                        x.Count() > 1);

            if (duplicateLine)
            {
                throw new InvalidOperationException(
                    "The same receipt line cannot be returned more than once in a single return.");
            }

            // -------------------------------------------------
            // Create return header
            // -------------------------------------------------

            var purchaseReturn =
                new PurchaseReturn
                {
                    ReturnNumber =
                        await GenerateReturnNumberAsync(
                            cancellationToken),

                    GoodsReceiptId =
                        receipt.Id,

                    SupplierId =
                        receipt.SupplierId,

                    LocationId =
                        receipt.LocationId,

                    ReturnDate =
                        request.ReturnDate,

                    Status =
                        PurchaseReturnStatus.Completed,

                    Reason =
                        NormalizeOptional(
                            request.Reason),

                    Notes =
                        NormalizeOptional(
                            request.Notes)
                };

            _dbContext.PurchaseReturns.Add(
                purchaseReturn);

            decimal totalAmount = 0;
            decimal totalQuantity = 0;

            // -------------------------------------------------
            // Process lines
            // -------------------------------------------------

            foreach (var requestLine
                     in request.Lines)
            {
                var receiptLine =
                    receipt.Lines
                        .FirstOrDefault(x =>
                            x.Id ==
                            requestLine.GoodsReceiptLineId);

                if (receiptLine is null)
                {
                    throw new InvalidOperationException(
                        "One of the selected receipt lines does not belong to this goods receipt.");
                }

                // ---------------------------------------------
                // Load exact batch
                // ---------------------------------------------

                var batch =
                    await _dbContext.InventoryBatches
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                    requestLine.InventoryBatchId &&
                                x.GoodsReceiptLineId ==
                                    receiptLine.Id &&
                                x.ProductVariantId ==
                                    receiptLine.ProductVariantId &&
                                x.LocationId ==
                                    receipt.LocationId &&
                                x.SupplierId ==
                                    receipt.SupplierId,
                            cancellationToken);

                if (batch is null)
                {
                    throw new InvalidOperationException(
                        "The inventory batch for one of the returned products could not be found or does not match the original receipt.");
                }

                // ---------------------------------------------
                // Document return validation
                // ---------------------------------------------

                var previouslyReturned =
                    await _dbContext.PurchaseReturnLines
                        .AsNoTracking()
                        .Where(x =>
                            x.GoodsReceiptLineId ==
                                receiptLine.Id &&
                            x.PurchaseReturn != null &&
                            x.PurchaseReturn.Status ==
                                PurchaseReturnStatus.Completed)
                        .SumAsync(
                            x => (decimal?)x.Quantity,
                            cancellationToken)
                    ?? 0;

                var documentRemaining =
                    receiptLine.ReceivedQuantity -
                    previouslyReturned;

                if (requestLine.Quantity >
                    documentRemaining)
                {
                    throw new InvalidOperationException(
                        $"Return quantity cannot exceed the remaining returnable quantity of {documentRemaining:0.###}.");
                }

                // ---------------------------------------------
                // Physical batch validation
                // ---------------------------------------------

                if (requestLine.Quantity >
                    batch.QuantityOnHand)
                {
                    throw new InvalidOperationException(
                        $"Only {batch.QuantityOnHand:0.###} units remain in batch {batch.SupplierBatchNumber}.");
                }

                // ---------------------------------------------
                // Stock balance
                // ---------------------------------------------

                var stockBalance =
                    await _dbContext.StockBalances
                        .FirstOrDefaultAsync(
                            x =>
                                x.ProductVariantId ==
                                    receiptLine.ProductVariantId &&
                                x.LocationId ==
                                    receipt.LocationId,
                            cancellationToken);

                if (stockBalance is null)
                {
                    throw new InvalidOperationException(
                        "Stock balance could not be found for one of the returned products.");
                }

                if (requestLine.Quantity >
                    stockBalance.QuantityOnHand)
                {
                    throw new InvalidOperationException(
                        "Return quantity exceeds the current stock balance.");
                }

                // ---------------------------------------------
                // Financial value
                // ---------------------------------------------

                var lineTotal =
                    requestLine.Quantity *
                    receiptLine.UnitCost;

                // ---------------------------------------------
                // Return line
                // ---------------------------------------------

                var returnLine =
                    new PurchaseReturnLine
                    {
                        PurchaseReturn =
                            purchaseReturn,

                        GoodsReceiptLineId =
                            receiptLine.Id,

                        ProductVariantId =
                            receiptLine.ProductVariantId,

                        InventoryBatchId =
                            batch.Id,

                        Quantity =
                            requestLine.Quantity,

                        UnitCost =
                            receiptLine.UnitCost,

                        LineTotal =
                            lineTotal,

                        Reason =
                            NormalizeOptional(
                                requestLine.Reason)
                    };

                purchaseReturn.Lines.Add(
                    returnLine);

                // ---------------------------------------------
                // Reduce exact inventory batch
                // ---------------------------------------------

                batch.QuantityOnHand -=
                    requestLine.Quantity;

                // ---------------------------------------------
                // Reduce stock balance
                // ---------------------------------------------

                stockBalance.QuantityOnHand -=
                    requestLine.Quantity;

                // ---------------------------------------------
                // Inventory ledger
                // ---------------------------------------------

                var movement =
                    new StockMovement
                    {
                        ProductVariantId =
                            receiptLine.ProductVariantId,

                        LocationId =
                            receipt.LocationId,

                        InventoryBatchId =
                            batch.Id,

                        MovementType =
                            InventoryMovementType.SupplierReturn,

                        Quantity =
                            -requestLine.Quantity,

                        UnitCost =
                            receiptLine.UnitCost,

                        ReferenceType =
                            "PurchaseReturn",

                        ReferenceId =
                            purchaseReturn.Id,

                        Notes =
                            $"Supplier return {purchaseReturn.ReturnNumber} " +
                            $"for goods receipt {receipt.ReceiptNumber}"
                    };

                _dbContext.StockMovements.Add(
                    movement);

                totalQuantity +=
                    requestLine.Quantity;

                totalAmount +=
                    lineTotal;
            }

            // -------------------------------------------------
            // Header totals
            // -------------------------------------------------

            purchaseReturn.TotalAmount =
                totalAmount;

            // -------------------------------------------------
            // Persist atomically
            // -------------------------------------------------

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new PurchaseReturnResultDto
            {
                PurchaseReturnId =
                    purchaseReturn.Id,

                ReturnNumber =
                    purchaseReturn.ReturnNumber,

                TotalQuantity =
                    totalQuantity,

                TotalAmount =
                    totalAmount
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static void ValidateRequest(
        CreatePurchaseReturnRequest request)
    {
        if (request.GoodsReceiptId ==
            Guid.Empty)
        {
            throw new InvalidOperationException(
                "Goods receipt is required.");
        }

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Select at least one product to return.");
        }

        foreach (var line in request.Lines)
        {
            if (line.GoodsReceiptLineId ==
                Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Goods receipt line is required.");
            }

            if (line.InventoryBatchId ==
                Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Inventory batch is required.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Return quantity must be greater than zero.");
            }
        }
    }

    // =========================================================
    // HISTORY
    // =========================================================

    public async Task<IReadOnlyList<PurchaseReturnHistoryDto>>
    GetHistoryAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseReturns
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.ReturnNumber.Contains(term) ||
                (x.Supplier != null &&
                 x.Supplier.Name.Contains(term)) ||
                (x.GoodsReceipt != null &&
                 x.GoodsReceipt.ReceiptNumber.Contains(term)));
        }

        return await query
            .OrderByDescending(x => x.ReturnDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new PurchaseReturnHistoryDto
            {
                Id = x.Id,

                ReturnNumber = x.ReturnNumber,

                ReturnDate = x.ReturnDate,

                ReceiptNumber =
                    x.GoodsReceipt != null
                        ? x.GoodsReceipt.ReceiptNumber
                        : string.Empty,

                SupplierName =
                    x.Supplier != null
                        ? x.Supplier.Name
                        : string.Empty,

                LocationName =
                    x.Location != null
                        ? x.Location.Name
                        : string.Empty,

                Status = x.Status,

                TotalQuantity =
                    x.Lines.Sum(line => line.Quantity),

                TotalAmount = x.TotalAmount,

                Reason = x.Reason
            })
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // DETAILS
    // =========================================================

    public async Task<PurchaseReturnDetailsDto?>
    GetByIdAsync(
        Guid purchaseReturnId,
        CancellationToken cancellationToken = default)
    {
        if (purchaseReturnId == Guid.Empty)
            return null;

        return await _dbContext.PurchaseReturns
            .AsNoTracking()
            .Where(x => x.Id == purchaseReturnId)
            .Select(x => new PurchaseReturnDetailsDto
            {
                Id = x.Id,

                ReturnNumber = x.ReturnNumber,

                ReturnDate = x.ReturnDate,

                Status = x.Status,

                GoodsReceiptId = x.GoodsReceiptId,

                ReceiptNumber =
                    x.GoodsReceipt != null
                        ? x.GoodsReceipt.ReceiptNumber
                        : string.Empty,

                PurchaseOrderNumber =
                    x.GoodsReceipt != null &&
                    x.GoodsReceipt.PurchaseOrder != null
                        ? x.GoodsReceipt.PurchaseOrder.OrderNumber
                        : string.Empty,

                SupplierId = x.SupplierId,

                SupplierName =
                    x.Supplier != null
                        ? x.Supplier.Name
                        : string.Empty,

                LocationId = x.LocationId,

                LocationName =
                    x.Location != null
                        ? x.Location.Name
                        : string.Empty,

                TotalQuantity =
                    x.Lines.Sum(line => line.Quantity),

                TotalAmount = x.TotalAmount,

                Reason = x.Reason,

                Notes = x.Notes,

                Lines = x.Lines
                    .OrderBy(line => line.CreatedAtUtc)
                    .Select(line =>
                        new PurchaseReturnDetailsLineDto
                        {
                            Id = line.Id,

                            GoodsReceiptLineId =
                                line.GoodsReceiptLineId,

                            ProductVariantId =
                                line.ProductVariantId,

                            InventoryBatchId =
                                line.InventoryBatchId,

                            ProductName =
                                line.ProductVariant != null &&
                                line.ProductVariant.Product != null
                                    ? line.ProductVariant.Product.Name
                                    : string.Empty,

                            SKU =
                                line.ProductVariant != null
                                    ? line.ProductVariant.SKU
                                    : string.Empty,

                            SupplierBatchNumber =
                                line.InventoryBatch != null
                                    ? line.InventoryBatch.SupplierBatchNumber
                                    : string.Empty,

                            Quantity =
                                line.Quantity,

                            UnitCost =
                                line.UnitCost,

                            LineTotal =
                                line.LineTotal,

                            Reason =
                                line.Reason
                        })
                    .ToList()
            })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    // =========================================================
    // NUMBER
    // =========================================================

    private async Task<string>
        GenerateReturnNumberAsync(
            CancellationToken cancellationToken)
    {
        var prefix =
            $"PR-{DateTime.UtcNow:yyyyMMdd}";

        var count =
            await _dbContext.PurchaseReturns
                .CountAsync(
                    x =>
                        x.ReturnNumber
                            .StartsWith(prefix),
                    cancellationToken);

        return $"{prefix}-{count + 1:000}";
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}