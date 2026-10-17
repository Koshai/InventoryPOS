using InventoryPOS.Application.Licensing;
using System.Data;
using InventoryPOS.Application.DTOs.WarrantyClaims;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class WarrantyReplacementService
    : IWarrantyReplacementService
{
    private readonly InventoryPosDbContext _db;

    private readonly ILicenseAccessGuard _licenseAccessGuard;


    public WarrantyReplacementService(
        InventoryPosDbContext db,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _licenseAccessGuard = licenseAccessGuard ?? throw new ArgumentNullException(nameof(licenseAccessGuard));
        _db = db;
    }

    public async Task<IssueWarrantyReplacementResult> IssueAsync(
        IssueWarrantyReplacementRequest request,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        if (request.ClaimId == Guid.Empty ||
            request.ClaimItemId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A claim and claim item are required.");
        }

        if (request.Quantity <= 0 ||
            decimal.Round(
                request.Quantity,
                3) != request.Quantity)
        {
            throw new InvalidOperationException(
                "Replacement quantity must be positive " +
                "and have no more than three decimal places.");
        }

        var notes =
            request.Notes?.Trim();

        if (notes?.Length > 2000)
        {
            throw new InvalidOperationException(
                "Replacement notes cannot exceed 2000 characters.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        // =========================================================
        // REFRESH TRACKED CLAIM STATE
        // =========================================================
        //
        // Warranty workflows can update the same claim through
        // different services during one desktop session.
        //
        // The DbContext is currently scoped for the desktop session,
        // so a previously tracked WarrantyClaim can contain stale
        // state. Detach it before performing the explicit database
        // lock/read below.
        //
        // This is intentionally retained until the broader DbContext
        // lifetime is moved toward a factory/per-operation model.
        // =========================================================

        var trackedClaim =
            _db.ChangeTracker
                .Entries<WarrantyClaim>()
                .FirstOrDefault(
                    x =>
                        x.Entity.Id ==
                        request.ClaimId);

        if (trackedClaim is not null)
        {
            trackedClaim.State =
                EntityState.Detached;
        }

        // =========================================================
        // LOCK CLAIM
        // =========================================================

        var claim =
            await _db.WarrantyClaims
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM "WarrantyClaims"
                    WHERE "Id" = {request.ClaimId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (claim is null)
        {
            throw new InvalidOperationException(
                "Warranty claim not found.");
        }

        if (claim.Status !=
            WarrantyClaimStatus.AwaitingReplacement)
        {
            throw new InvalidOperationException(
                "This claim is not awaiting replacement.");
        }

        // =========================================================
        // LOAD CLAIM ITEMS
        // =========================================================

        var claimItems =
            await _db.WarrantyClaimItems
                .Where(
                    x =>
                        x.WarrantyClaimId ==
                        claim.Id)
                .ToListAsync(
                    cancellationToken);

        var item =
            claimItems.SingleOrDefault(
                x =>
                    x.Id ==
                    request.ClaimItemId);

        if (item is null)
        {
            throw new InvalidOperationException(
                "The selected item does not belong to this claim.");
        }

        if (item.ResolutionType !=
            WarrantyResolutionType.Pending)
        {
            throw new InvalidOperationException(
                "This item has already been resolved.");
        }

        // =========================================================
        // PREVIOUS REPLACEMENT FULFILLMENT
        // =========================================================

        var previouslyIssued =
            await _db.WarrantyReplacements
                .Where(
                    x =>
                        x.WarrantyClaimItemId ==
                        item.Id)
                .SumAsync(
                    x => (decimal?)x.Quantity,
                    cancellationToken)
            ?? 0m;

        var remainingApproved =
            item.Quantity -
            previouslyIssued;

        if (remainingApproved <= 0 ||
            request.Quantity >
            remainingApproved)
        {
            throw new InvalidOperationException(
                $"Requested quantity exceeds the remaining " +
                $"approved quantity ({remainingApproved:N3}).");
        }

        // =========================================================
        // ORIGINAL PRODUCT VARIANT
        // =========================================================

        var originalVariantId =
            await _db.SaleLines
                .Where(
                    x =>
                        x.Id ==
                        item.SaleLineId)
                .Select(
                    x =>
                        x.ProductVariantId)
                .SingleAsync(
                    cancellationToken);

        // =========================================================
        // LOCK STOCK BALANCE
        // =========================================================

        var balance =
            await _db.StockBalances
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM "StockBalances"
                    WHERE "ProductVariantId" = {originalVariantId}
                      AND "LocationId" = {claim.LocationId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (balance is null)
        {
            throw new InvalidOperationException(
                "No stock balance exists for this replacement.");
        }

        // =========================================================
        // LOCK ALL BATCHES
        // =========================================================
        //
        // All batches are locked, including expired batches.
        //
        // We need the complete batch quantity to distinguish:
        //
        //   1. inventory represented by batches
        //   2. older/legacy unbatched inventory
        //
        // Expired batch quantities must NOT accidentally be treated
        // as legacy stock.
        // =========================================================

        var batches =
            await _db.InventoryBatches
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM "InventoryBatches"
                    WHERE "ProductVariantId" = {originalVariantId}
                      AND "LocationId" = {claim.LocationId}
                    ORDER BY "ExpiryDate" ASC NULLS LAST,
                             "CreatedAtUtc" ASC,
                             "Id" ASC
                    FOR UPDATE
                    """)
                .ToListAsync(
                    cancellationToken);

        var batchedOnHand =
            batches.Sum(
                x =>
                    x.QuantityOnHand);

        var legacyOnHand =
            balance.QuantityOnHand -
            batchedOnHand;

        if (legacyOnHand < 0)
        {
            throw new InvalidOperationException(
                "Inventory balance and batch quantities " +
                "are inconsistent. Reconcile stock first.");
        }

        // =========================================================
        // NON-EXPIRED ELIGIBLE STOCK
        // =========================================================

        var today =
            DateTime.UtcNow.Date;

        var eligibleBatches =
            batches
                .Where(
                    x =>
                        x.QuantityOnHand > 0 &&
                        (!x.ExpiryDate.HasValue ||
                         x.ExpiryDate.Value.Date >= today))
                .ToList();

        // =========================================================
        // AVAILABLE REPLACEMENT STOCK
        // =========================================================
        //
        // QuantityOnHand can include expired batch quantities.
        // Therefore replacement availability must be calculated
        // from stock that is actually eligible to issue.
        //
        // Reservations are currently aggregate at the stock-balance
        // level rather than batch-specific. Treat them as a protected
        // non-consumable buffer.
        // =========================================================

        var eligibleBatchedOnHand =
            eligibleBatches.Sum(
                x =>
                    x.QuantityOnHand);

        var physicallyEligible =
            eligibleBatchedOnHand +
            legacyOnHand;

        var available =
            Math.Max(
                0m,
                physicallyEligible -
                balance.QuantityReserved);

        if (available <
            request.Quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient non-expired replacement stock. " +
                $"Available: {available:N3}.");
        }

        // =========================================================
        // FEFO ALLOCATION
        // =========================================================

        var remaining =
            request.Quantity;

        var movementCount =
            0;

        var issuedAt =
            DateTime.UtcNow;

        foreach (var batch in eligibleBatches)
        {
            if (remaining <= 0)
                break;

            var quantity =
                Math.Min(
                    remaining,
                    batch.QuantityOnHand);

            batch.QuantityOnHand -=
                quantity;

            remaining -=
                quantity;

            AddMovementAndReplacement(
                claim,
                item,
                originalVariantId,
                batch.Id,
                quantity,
                batch.UnitCost,
                issuedAt,
                notes);

            movementCount++;
        }

        // =========================================================
        // LEGACY UNBATCHED STOCK
        // =========================================================
        //
        // legacyOnHand was calculated before batch deductions.
        //
        // Expired batch quantities were included in batchedOnHand,
        // which means they can never be misclassified as legacy
        // inventory.
        // =========================================================

        if (remaining > 0)
        {
            if (remaining >
                legacyOnHand)
            {
                throw new InvalidOperationException(
                    "Insufficient non-expired replacement stock.");
            }

            AddMovementAndReplacement(
                claim,
                item,
                originalVariantId,
                null,
                remaining,
                null,
                issuedAt,
                notes);

            movementCount++;

            remaining =
                0;
        }

        // =========================================================
        // UPDATE AGGREGATE STOCK BALANCE
        // =========================================================

        balance.QuantityOnHand -=
            request.Quantity;

        // =========================================================
        // ITEM FULFILLMENT
        // =========================================================

        var issuedForThisItem =
            previouslyIssued +
            request.Quantity;

        if (issuedForThisItem ==
            item.Quantity)
        {
            item.ResolutionType =
                WarrantyResolutionType.Replaced;
        }

        // =========================================================
        // CLAIM-WIDE FULFILLMENT
        // =========================================================
        //
        // Load previous persisted allocations for every item in the
        // claim, then include the allocation made by this transaction
        // when calculating whether the whole claim is complete.
        // =========================================================

        var existingAllocations =
            await _db.WarrantyReplacements
                .Where(
                    x =>
                        x.WarrantyClaimItem
                            .WarrantyClaimId ==
                        claim.Id)
                .Select(
                    x => new
                    {
                        x.WarrantyClaimItemId,
                        x.Quantity
                    })
                .ToListAsync(
                    cancellationToken);

        var issuedByItem =
            existingAllocations
                .GroupBy(
                    x =>
                        x.WarrantyClaimItemId)
                .ToDictionary(
                    x =>
                        x.Key,
                    x =>
                        x.Sum(
                            y =>
                                y.Quantity));

        issuedByItem[item.Id] =
            issuedForThisItem;

        var claimResolved =
            claimItems.All(
                x =>
                    issuedByItem
                        .GetValueOrDefault(
                            x.Id) ==
                    x.Quantity);

        if (claimResolved)
        {
            foreach (var claimItem in claimItems)
            {
                claimItem.ResolutionType =
                    WarrantyResolutionType.Replaced;
            }

            claim.Status =
                WarrantyClaimStatus.Resolved;

            claim.ResolvedAtUtc =
                issuedAt;

            claim.ResolutionNotes =
                "All approved warranty replacements issued.";
        }

        // =========================================================
        // AUDIT EVENT - REPLACEMENT ISSUED
        // =========================================================
        //
        // ReplacementIssued describes the business operation.
        //
        // It deliberately does NOT own the state transition to
        // Resolved. ClaimResolved below owns that transition.
        //
        // This mirrors the repair timeline semantics:
        //
        // RepairCompleted:
        //     InRepair -> InRepair
        //
        // ClaimResolved:
        //     InRepair -> Resolved
        // =========================================================

        _db.WarrantyClaimEvents.Add(
            new WarrantyClaimEvent
            {
                WarrantyClaimId =
                    claim.Id,

                EventType =
                    WarrantyClaimEventType
                        .ReplacementIssued,

                FromStatus =
                    WarrantyClaimStatus
                        .AwaitingReplacement,

                ToStatus =
                    WarrantyClaimStatus
                        .AwaitingReplacement,

                Description =
                    $"Warranty replacement issued: " +
                    $"{request.Quantity:N3} unit(s).",

                Notes =
                    notes
            });

        // =========================================================
        // AUDIT EVENT - CLAIM RESOLVED
        // =========================================================

        if (claimResolved)
        {
            _db.WarrantyClaimEvents.Add(
                new WarrantyClaimEvent
                {
                    WarrantyClaimId =
                        claim.Id,

                    EventType =
                        WarrantyClaimEventType
                            .ClaimResolved,

                    FromStatus =
                        WarrantyClaimStatus
                            .AwaitingReplacement,

                    ToStatus =
                        WarrantyClaimStatus
                            .Resolved,

                    Description =
                        "Warranty claim resolved after replacement.",

                    Notes =
                        "All approved warranty replacements issued."
                });
        }

        // =========================================================
        // COMMIT
        // =========================================================
        //
        // Inventory batches, stock balance, stock movements,
        // replacement allocations, claim/item resolution state and
        // audit events all participate in this same transaction.
        // =========================================================

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return new IssueWarrantyReplacementResult
        {
            ClaimId =
                claim.Id,

            ClaimItemId =
                item.Id,

            QuantityIssued =
                request.Quantity,

            RemainingQuantity =
                item.Quantity -
                issuedForThisItem,

            ClaimResolved =
                claimResolved,

            StockMovementsCreated =
                movementCount
        };
    }

    // =============================================================
    // STOCK MOVEMENT + REPLACEMENT ALLOCATION
    // =============================================================

    private void AddMovementAndReplacement(
        WarrantyClaim claim,
        WarrantyClaimItem item,
        Guid variantId,
        Guid? batchId,
        decimal quantity,
        decimal? unitCost,
        DateTime issuedAt,
        string? notes)
    {
        var movement =
            new StockMovement
            {
                ProductVariantId =
                    variantId,

                LocationId =
                    claim.LocationId,

                InventoryBatchId =
                    batchId,

                MovementType =
                    InventoryMovementType
                        .WarrantyReplacement,

                Quantity =
                    -quantity,

                UnitCost =
                    unitCost,

                ReferenceType =
                    "WarrantyClaim",

                ReferenceId =
                    claim.Id,

                Notes =
                    $"Replacement for " +
                    $"{claim.ClaimNumber}"
            };

        _db.StockMovements.Add(
            movement);

        _db.WarrantyReplacements.Add(
            new WarrantyReplacement
            {
                WarrantyClaimItemId =
                    item.Id,

                ReplacementVariantId =
                    variantId,

                LocationId =
                    claim.LocationId,

                InventoryBatchId =
                    batchId,

                StockMovement =
                    movement,

                Quantity =
                    quantity,

                IssuedAtUtc =
                    issuedAt,

                Notes =
                    notes
            });
    }
}