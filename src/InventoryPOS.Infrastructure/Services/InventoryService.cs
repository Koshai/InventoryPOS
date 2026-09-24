using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly InventoryPosDbContext _dbContext;

    public InventoryService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InventoryItemDto>>
        GetInventoryAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockBalances
            .AsNoTracking()
            .OrderBy(x => x.ProductVariant!.Product!.Name)
            .Select(x => new InventoryItemDto
            {
                ProductVariantId = x.ProductVariantId,

                LocationId = x.LocationId,

                ProductName =
                    x.ProductVariant!.Product!.Name,

                SKU =
                    x.ProductVariant.SKU,

                LocationName =
                    x.Location!.Name,

                UnitSymbol =
                    x.ProductVariant.Product!.Unit!.Symbol,

                QuantityOnHand =
                    x.QuantityOnHand,

                QuantityReserved =
                    x.QuantityReserved,

                SellingPrice =
                    x.ProductVariant.SellingPrice,

                ReorderLevel =
                    x.ProductVariant.ReorderLevel
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SetOpeningStockAsync(
        SetOpeningStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException(
                "Opening stock quantity must be greater than zero.");
        }

        if (request.UnitCost.HasValue &&
            request.UnitCost.Value < 0)
        {
            throw new ArgumentException(
                "Unit cost cannot be negative.");
        }

        var variantExists =
            await _dbContext.ProductVariants
                .AnyAsync(
                    x =>
                        x.Id == request.ProductVariantId &&
                        x.IsActive &&
                        x.Product!.IsActive,
                    cancellationToken);

        if (!variantExists)
        {
            throw new InvalidOperationException(
                "The selected product variant is invalid.");
        }

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
                "The selected location is invalid.");
        }

        var openingStockAlreadyExists =
            await _dbContext.StockMovements
                .AnyAsync(
                    x =>
                        x.ProductVariantId ==
                            request.ProductVariantId
                        &&
                        x.LocationId ==
                            request.LocationId
                        &&
                        x.MovementType ==
                            InventoryMovementType.OpeningStock,
                    cancellationToken);

        if (openingStockAlreadyExists)
        {
            throw new InvalidOperationException(
                "Opening stock has already been entered for this product at this location. Use a stock adjustment instead.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var balance =
                await _dbContext.StockBalances
                    .FirstOrDefaultAsync(
                        x =>
                            x.ProductVariantId ==
                                request.ProductVariantId
                            &&
                            x.LocationId ==
                                request.LocationId,
                        cancellationToken);

            if (balance is null)
            {
                balance = new StockBalance
                {
                    ProductVariantId =
                        request.ProductVariantId,

                    LocationId =
                        request.LocationId,

                    QuantityOnHand =
                        request.Quantity,

                    QuantityReserved = 0
                };

                _dbContext.StockBalances.Add(
                    balance);
            }
            else
            {
                balance.QuantityOnHand +=
                    request.Quantity;
            }

            var movement =
                new StockMovement
                {
                    ProductVariantId =
                        request.ProductVariantId,

                    LocationId =
                        request.LocationId,

                    MovementType =
                        InventoryMovementType.OpeningStock,

                    Quantity =
                        request.Quantity,

                    UnitCost =
                        request.UnitCost,

                    ReferenceType =
                        "OpeningStock",

                    Notes =
                        string.IsNullOrWhiteSpace(
                            request.Notes)
                            ? "Initial opening stock"
                            : request.Notes.Trim()
                };

            _dbContext.StockMovements.Add(
                movement);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<IReadOnlyList<InventoryProductLookupDto>>
    GetProductVariantsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Product!.IsActive)
            .OrderBy(x => x.Product!.Name)
            .ThenBy(x => x.SKU)
            .Select(x =>
                new InventoryProductLookupDto
                {
                    ProductVariantId = x.Id,

                    ProductName =
                        x.Product!.Name,

                    SKU =
                        x.SKU,

                    UnitSymbol =
                        x.Product.Unit!.Symbol
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
            .Select(x =>
                new LocationLookupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryLedgerItemDto>>
    GetInventoryLedgerAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .AsNoTracking()
            .Include(x => x.ProductVariant)
                .ThenInclude(x => x!.Product)
            .Include(x => x.ProductVariant)
                .ThenInclude(x => x!.Product)
                    .ThenInclude(x => x!.Unit)
            .Include(x => x.Location)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new InventoryLedgerItemDto
            {
                Id = x.Id,

                CreatedAt = x.CreatedAtUtc,

                ProductVariantId =
                    x.ProductVariantId,

                LocationId =
                    x.LocationId,

                ProductName =
                    x.ProductVariant != null &&
                    x.ProductVariant.Product != null
                        ? x.ProductVariant.Product.Name
                        : string.Empty,

                SKU =
                    x.ProductVariant != null
                        ? x.ProductVariant.SKU
                        : string.Empty,

                LocationName =
                    x.Location != null
                        ? x.Location.Name
                        : string.Empty,

                UnitSymbol =
                    x.ProductVariant != null &&
                    x.ProductVariant.Product != null &&
                    x.ProductVariant.Product.Unit != null
                        ? x.ProductVariant.Product.Unit.Symbol
                        : string.Empty,

                MovementType =
                    x.MovementType.ToString(),

                Quantity =
                    x.Quantity,

                UnitCost =
                    x.UnitCost,

                ReferenceType =
                    x.ReferenceType,

                ReferenceId =
                    x.ReferenceId,

                Notes =
                    x.Notes
            })
            .ToListAsync(cancellationToken);
    }
}