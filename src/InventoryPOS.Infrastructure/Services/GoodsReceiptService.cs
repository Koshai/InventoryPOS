using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class GoodsReceiptService : IGoodsReceiptService
{
    private readonly InventoryPosDbContext _dbContext;
    private readonly ILicenseAccessGuard _licenseAccessGuard;

    public GoodsReceiptService(
        InventoryPosDbContext dbContext,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _dbContext = dbContext;
        _licenseAccessGuard = licenseAccessGuard;
    }

    public async Task<ReceivePurchaseOrderDto?>
        GetPurchaseOrderForReceiptAsync(
            Guid purchaseOrderId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == purchaseOrderId)
            .Select(x => new ReceivePurchaseOrderDto
            {
                PurchaseOrderId = x.Id,

                OrderNumber = x.OrderNumber,

                SupplierId = x.SupplierId,

                SupplierCode =
                    x.Supplier!.SupplierCode,

                SupplierName =
                    x.Supplier.Name,

                LocationId = x.LocationId,

                LocationName =
                    x.Location!.Name,

                Lines = x.Lines
                    .Where(line =>
                        line.OrderedQuantity >
                        line.ReceivedQuantity)
                    .OrderBy(line =>
                        line.ProductVariant!.Product!.Name)
                    .Select(line =>
                        new ReceivePurchaseOrderLineDto
                        {
                            PurchaseOrderLineId =
                                line.Id,

                            ProductVariantId =
                                line.ProductVariantId,

                            ProductName =
                                line.ProductVariant!
                                    .Product!.Name,

                            SKU =
                                line.ProductVariant.SKU,

                            UnitSymbol =
                                line.ProductVariant
                                    .Product.Unit!.Symbol,

                            OrderedQuantity =
                                line.OrderedQuantity,

                            ReceivedQuantity =
                                line.ReceivedQuantity,

                            UnitCost =
                                line.UnitCost,

                            WarrantyMonths =
                                line.WarrantyMonths
                        })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateReceiptAsync(
    CreateGoodsReceiptRequest request,
    CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();
        if (request.PurchaseOrderId == Guid.Empty)
            throw new InvalidOperationException(
                "Purchase order is required.");

        var receiptLines =
            request.Lines
                .Where(x => x.ReceivingQuantity > 0)
                .ToList();

        if (receiptLines.Count == 0)
            throw new InvalidOperationException(
                "Enter a receiving quantity for at least one product.");

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var purchaseOrder =
                await _dbContext.PurchaseOrders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(
                        x => x.Id == request.PurchaseOrderId,
                        cancellationToken);

            if (purchaseOrder is null)
                throw new InvalidOperationException(
                    "Purchase order could not be found.");

            if (purchaseOrder.Status ==
                PurchaseOrderStatus.Received)
            {
                throw new InvalidOperationException(
                    "This purchase order has already been fully received.");
            }

            if (purchaseOrder.Status ==
                PurchaseOrderStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "A cancelled purchase order cannot be received.");
            }

            var goodsReceipt =
                new GoodsReceipt
                {
                    PurchaseOrderId =
                        purchaseOrder.Id,

                    LocationId =
                        purchaseOrder.LocationId,

                    SupplierId =
                        purchaseOrder.SupplierId,

                    ReceiptNumber =
                        await GenerateReceiptNumberAsync(
                            cancellationToken),

                    ReceivedAt =
                        request.ReceiptDate,

                    Notes =
                        string.IsNullOrWhiteSpace(request.Notes)
                            ? null
                            : request.Notes.Trim()
                };

            _dbContext.GoodsReceipts.Add(
                goodsReceipt);

            foreach (var requestLine in receiptLines)
            {
                var poLine =
                    purchaseOrder.Lines
                        .FirstOrDefault(x =>
                            x.Id ==
                            requestLine.PurchaseOrderLineId);

                if (poLine is null)
                {
                    throw new InvalidOperationException(
                        "One of the purchase order lines no longer exists.");
                }

                var remaining =
                    poLine.OrderedQuantity -
                    poLine.ReceivedQuantity;

                if (requestLine.ReceivingQuantity >
                    remaining)
                {
                    throw new InvalidOperationException(
                        "Receiving quantity cannot exceed the remaining ordered quantity.");
                }

                if (string.IsNullOrWhiteSpace(
                        requestLine.SupplierBatchNumber))
                {
                    throw new InvalidOperationException(
                        "Supplier batch number is required for received products.");
                }

                if (requestLine.ManufactureDate.HasValue &&
                    requestLine.ExpiryDate.HasValue &&
                    requestLine.ExpiryDate.Value <
                    requestLine.ManufactureDate.Value)
                {
                    throw new InvalidOperationException(
                        "Expiry date cannot be earlier than manufacture date.");
                }

                var receiptLine =
                    new GoodsReceiptLine
                    {
                        GoodsReceipt =
                            goodsReceipt,

                        PurchaseOrderLineId =
                            poLine.Id,

                        ProductVariantId =
                            poLine.ProductVariantId,

                        ReceivedQuantity =
                            requestLine.ReceivingQuantity,

                        UnitCost =
                            poLine.UnitCost,

                        SupplierBatchNumber =
                            requestLine
                                .SupplierBatchNumber
                                .Trim(),

                        ManufactureDate =
                            requestLine.ManufactureDate,

                        ExpiryDate =
                            requestLine.ExpiryDate,

                        WarrantyMonths =
                            requestLine.WarrantyMonths
                            ?? poLine.WarrantyMonths
                    };

                goodsReceipt.Lines.Add(
                    receiptLine);

                poLine.ReceivedQuantity +=
                    requestLine.ReceivingQuantity;
            }

            /*
             * Save first so GoodsReceiptLine IDs are definitely
             * available before InventoryBatch references them.
             *
             * We are still inside the same DB transaction.
             */
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            foreach (var receiptLine in goodsReceipt.Lines)
            {
                var inventoryBatch =
                    new InventoryBatch
                    {
                        ProductVariantId =
                            receiptLine.ProductVariantId,

                        LocationId =
                            purchaseOrder.LocationId,

                        SupplierId =
                            purchaseOrder.SupplierId,

                        GoodsReceiptLineId =
                            receiptLine.Id,

                        SupplierBatchNumber =
                            receiptLine.SupplierBatchNumber!,

                        ManufactureDate =
                            receiptLine.ManufactureDate,

                        ExpiryDate =
                            receiptLine.ExpiryDate,

                        ReceivedQuantity =
                            receiptLine.ReceivedQuantity,

                        QuantityOnHand =
                            receiptLine.ReceivedQuantity,

                        UnitCost =
                            receiptLine.UnitCost
                    };

                _dbContext.InventoryBatches.Add(
                    inventoryBatch);

                await UpdateStockBalanceAsync(
                    receiptLine.ProductVariantId,
                    purchaseOrder.LocationId,
                    receiptLine.ReceivedQuantity,
                    cancellationToken);

                CreatePurchaseReceiptMovement(
                    receiptLine,
                    inventoryBatch,
                    purchaseOrder);
            }

            var fullyReceived =
                purchaseOrder.Lines.All(x =>
                    x.ReceivedQuantity >=
                    x.OrderedQuantity);

            purchaseOrder.Status =
                fullyReceived
                    ? PurchaseOrderStatus.Received
                    : PurchaseOrderStatus.PartiallyReceived;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return goodsReceipt.Id;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task UpdateStockBalanceAsync(
    Guid productVariantId,
    Guid locationId,
    decimal quantity,
    CancellationToken cancellationToken)
    {
        var balance =
            await _dbContext.StockBalances
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductVariantId == productVariantId &&
                        x.LocationId == locationId,
                    cancellationToken);

        if (balance is null)
        {
            balance = new StockBalance
            {
                ProductVariantId = productVariantId,
                LocationId = locationId,
                QuantityOnHand = quantity,
                QuantityReserved = 0
            };

            _dbContext.StockBalances.Add(balance);

            return;
        }

        balance.QuantityOnHand += quantity;
    }

    private void CreatePurchaseReceiptMovement(
    GoodsReceiptLine receiptLine,
    InventoryBatch inventoryBatch,
    PurchaseOrder purchaseOrder)
    {
        var movement =
            new StockMovement
            {
                ProductVariantId =
                    receiptLine.ProductVariantId,

                LocationId =
                    purchaseOrder.LocationId,

                InventoryBatch =
                    inventoryBatch,

                MovementType =
                    InventoryMovementType.PurchaseReceipt,

                Quantity =
                    receiptLine.ReceivedQuantity,

                UnitCost =
                    receiptLine.UnitCost,

                ReferenceType =
                    "GoodsReceipt",

                ReferenceId =
                    receiptLine.GoodsReceiptId,

                Notes =
                    $"Purchase receipt for {purchaseOrder.OrderNumber}"
            };

        _dbContext.StockMovements.Add(
            movement);
    }

    private async Task<string> GenerateReceiptNumberAsync(
    CancellationToken cancellationToken)
    {
        var prefix =
            $"GR-{DateTime.UtcNow:yyyyMMdd}";

        var count =
            await _dbContext.GoodsReceipts
                .CountAsync(
                    x =>
                        x.ReceiptNumber
                            .StartsWith(prefix),
                    cancellationToken);

        return $"{prefix}-{count + 1:000}";
    }

    public async Task<IReadOnlyList<GoodsReceiptListItemDto>>
    GetByPurchaseOrderAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(x => x.ReceivedAt)
            .Select(x => new GoodsReceiptListItemDto
            {
                Id = x.Id,
                ReceiptNumber = x.ReceiptNumber,
                ReceiptDate = x.ReceivedAt,

                LineCount = x.Lines.Count,

                TotalQuantity =
                    x.Lines.Sum(line =>
                        line.ReceivedQuantity),

                TotalValue =
                    x.Lines.Sum(line =>
                        line.ReceivedQuantity *
                        line.UnitCost)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GoodsReceiptDetailsDto?> GetByIdAsync(
    Guid goodsReceiptId,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(x => x.Id == goodsReceiptId)
            .Select(x => new GoodsReceiptDetailsDto
            {
                Id = x.Id,

                ReceiptNumber =
                    x.ReceiptNumber,

                ReceiptDate =
                    x.ReceivedAt,

                PurchaseOrderNumber =
                    x.PurchaseOrder!.OrderNumber,

                SupplierCode =
                    x.PurchaseOrder.Supplier!
                        .SupplierCode,

                SupplierName =
                    x.PurchaseOrder.Supplier.Name,

                LocationName =
                    x.Location!.Name,

                Notes =
                    x.Notes,

                Lines = x.Lines
                    .OrderBy(line =>
                        line.ProductVariant!
                            .Product!.Name)
                    .Select(line =>
                        new GoodsReceiptLineDetailsDto
                        {
                            Id = line.Id,

                            SKU =
                                line.ProductVariant!.SKU,

                            ProductName =
                                line.ProductVariant
                                    .Product!.Name,

                            ReceivedQuantity =
                                line.ReceivedQuantity,

                            UnitCost =
                                line.UnitCost,

                            SupplierBatchNumber =
                                line.SupplierBatchNumber
                                ?? string.Empty,

                            ManufactureDate =
                                line.ManufactureDate,

                            ExpiryDate =
                                line.ExpiryDate,

                            WarrantyMonths =
                                line.WarrantyMonths
                        })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}