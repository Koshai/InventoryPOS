using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly InventoryPosDbContext _dbContext;

    public StockAdjustmentService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockAdjustmentResultDto> CreateAsync(
        CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductVariantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Product is required.");
        }

        if (request.LocationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Location is required.");
        }

        if (request.CountedQuantity < 0)
        {
            throw new InvalidOperationException(
                "Counted quantity cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException(
                "Adjustment reason is required.");
        }

        var productVariant =
            await _dbContext.ProductVariants
                .Include(x => x.Product)
                .FirstOrDefaultAsync(
                    x => x.Id == request.ProductVariantId,
                    cancellationToken);

        if (productVariant is null ||
            productVariant.Product is null)
        {
            throw new InvalidOperationException(
                "Product could not be found.");
        }

        if (!productVariant.IsActive ||
            !productVariant.Product.IsActive)
        {
            throw new InvalidOperationException(
                "Inactive products cannot be adjusted.");
        }

        var location =
            await _dbContext.Locations
                .FirstOrDefaultAsync(
                    x => x.Id == request.LocationId,
                    cancellationToken);

        if (location is null)
        {
            throw new InvalidOperationException(
                "Location could not be found.");
        }

        if (!location.IsActive)
        {
            throw new InvalidOperationException(
                "Inactive locations cannot be adjusted.");
        }

        var stockBalance =
            await _dbContext.StockBalances
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductVariantId ==
                        request.ProductVariantId &&
                        x.LocationId ==
                        request.LocationId,
                    cancellationToken);

        if (stockBalance is null)
        {
            throw new InvalidOperationException(
                "No stock balance exists for this product at this location. Enter opening stock first.");
        }

        var systemQuantity =
            stockBalance.QuantityOnHand;

        var difference =
            request.CountedQuantity -
            systemQuantity;

        if (difference == 0)
        {
            throw new InvalidOperationException(
                "No adjustment is required because the counted quantity matches the current stock.");
        }

        var movementType =
            difference > 0
                ? InventoryMovementType.AdjustmentIncrease
                : InventoryMovementType.AdjustmentDecrease;

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var adjustment =
                new StockAdjustment
                {
                    LocationId =
                        request.LocationId,

                    Status =
                        StockAdjustmentStatus.Completed,

                    Reason =
                        request.Reason.Trim(),

                    Notes =
                        string.IsNullOrWhiteSpace(
                            request.Notes)
                            ? null
                            : request.Notes.Trim(),

                    CompletedAt =
                        DateTime.UtcNow
                };

            _dbContext.StockAdjustments.Add(
                adjustment);

            var adjustmentLine =
                new StockAdjustmentLine
                {
                    StockAdjustment =
                        adjustment,

                    ProductVariantId =
                        request.ProductVariantId,

                    SystemQuantity =
                        systemQuantity,

                    CountedQuantity =
                        request.CountedQuantity,

                    Difference =
                        difference
                };

            _dbContext.StockAdjustmentLines.Add(
                adjustmentLine);

            var movement =
                new StockMovement
                {
                    ProductVariantId =
                        request.ProductVariantId,

                    LocationId =
                        request.LocationId,

                    MovementType =
                        movementType,

                    Quantity =
                        difference,

                    ReferenceType =
                        "StockAdjustment",

                    ReferenceId =
                        adjustment.Id,

                    Notes =
                        BuildMovementNotes(
                            request.Reason,
                            request.Notes)
                };

            _dbContext.StockMovements.Add(
                movement);

            stockBalance.QuantityOnHand =
                request.CountedQuantity;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new StockAdjustmentResultDto
            {
                AdjustmentId =
                    adjustment.Id,

                PreviousQuantity =
                    systemQuantity,

                CountedQuantity =
                    request.CountedQuantity,

                Difference =
                    difference,

                MovementType =
                    movementType.ToString()
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<decimal?> GetCurrentQuantityAsync(
    Guid productVariantId,
    Guid locationId,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockBalances
            .AsNoTracking()
            .Where(x =>
                x.ProductVariantId == productVariantId &&
                x.LocationId == locationId)
            .Select(x => (decimal?)x.QuantityOnHand)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StockAdjustmentDetailsDto?> GetByIdAsync(
    Guid adjustmentId,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockAdjustments
            .AsNoTracking()
            .Where(x => x.Id == adjustmentId)
            .Select(x => new StockAdjustmentDetailsDto
            {
                Id = x.Id,

                CreatedAt = x.CreatedAtUtc,

                CompletedAt = x.CompletedAt,

                Status = x.Status.ToString(),

                LocationId = x.LocationId,

                LocationName =
                    x.Location != null
                        ? x.Location.Name
                        : string.Empty,

                Reason = x.Reason,

                Notes = x.Notes,

                Lines = x.Lines
                    .Select(line => new StockAdjustmentLineDto
                    {
                        Id = line.Id,

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

                        SystemQuantity =
                            line.SystemQuantity,

                        CountedQuantity =
                            line.CountedQuantity,

                        Difference =
                            line.Difference
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    private static string BuildMovementNotes(
        string reason,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return reason.Trim();
        }

        return $"{reason.Trim()} - {notes.Trim()}";
    }
}