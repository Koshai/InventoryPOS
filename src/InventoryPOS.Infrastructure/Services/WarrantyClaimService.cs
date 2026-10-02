using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Warranty;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventoryPOS.Infrastructure.Services;

public sealed class WarrantyClaimService : IWarrantyClaimService
{
    private readonly InventoryPosDbContext _dbContext;

    public WarrantyClaimService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterWarrantyClaimResultDto> RegisterAsync(
        RegisterWarrantyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SaleId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "An original sale is required.");
        }

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "Select at least one product to claim.");
        }

        if (request.CustomerComplaint?.Length > 2000)
        {
            throw new InvalidOperationException(
                "The customer complaint cannot exceed " +
                "2000 characters.");
        }

        if (request.Items.Any(x =>
        x.SaleLineId == Guid.Empty ||
        x.Quantity <= 0 ||
        HasMoreThanThreeDecimalPlaces(
            x.Quantity) ||
        x.ReportedDefect?.Length > 2000 ||
        x.SerialNumber?.Length > 150))
        {
            throw new InvalidOperationException(
                "Check the claimed quantities, " +
                "serial numbers and defects. " +
                "Quantities may use at most 3 decimal places.");
        }

        if (request.Items
            .GroupBy(x => x.SaleLineId)
            .Any(g => g.Count() > 1))
        {
            throw new InvalidOperationException(
                "Each sale line may appear only once per claim.");
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        try
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var sale = await _dbContext.Sales
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == request.SaleId,
                    cancellationToken);

            if (sale is null)
            {
                throw new InvalidOperationException(
                    "The original sale was not found.");
            }

            var saleLineIds = request.Items
                .Select(x => x.SaleLineId)
                .ToArray();

            var saleLines = await _dbContext.SaleLines
                .AsNoTracking()
                .Where(x =>
                    x.SaleId == request.SaleId &&
                    saleLineIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

            if (saleLines.Count != saleLineIds.Length)
            {
                throw new InvalidOperationException(
                    "One or more products do not belong " +
                    "to this invoice.");
            }

            // Read previous claims in the same
            // serializable transaction.
            var previousItems =
                await _dbContext.WarrantyClaimItems
                    .AsNoTracking()
                    .Include(x => x.WarrantyClaim)
                    .Where(x =>
                        saleLineIds.Contains(x.SaleLineId))
                    .ToListAsync(cancellationToken);

            var claim = new WarrantyClaim
            {
                // Globally unique, independent of simultaneous
                // registrations on different computers.
                ClaimNumber =
                    $"WCL-{Guid.NewGuid():N}",

                SaleId = sale.Id,
                CustomerId = sale.CustomerId,
                LocationId = sale.LocationId,
                ClaimDate = now,

                Status =
                    WarrantyClaimStatus.Submitted,

                CustomerComplaint =
                    request.CustomerComplaint?.Trim()
            };

            foreach (var requestedItem in request.Items)
            {
                var saleLine =
                    saleLines[requestedItem.SaleLineId];

                ValidateWarranty(
                    saleLine,
                    today);

                var previousForLine = previousItems
                    .Where(x =>
                        x.SaleLineId ==
                        requestedItem.SaleLineId)
                    .ToList();

                var occupiedQuantity =
                    previousForLine
                        .Where(ConsumesClaimQuantity)
                        .Sum(x => x.Quantity);

                var availableQuantity =
                    saleLine.Quantity -
                    occupiedQuantity;

                if (availableQuantity <= 0)
                {
                    throw new InvalidOperationException(
                        "No quantity remains available for a warranty claim " +
                        "on this invoice line.");
                }

                if (requestedItem.Quantity >
                    availableQuantity)
                {
                    throw new InvalidOperationException(
                        $"Only " +
                        $"{availableQuantity:0.###} " +
                        "unit(s) remain available for a claim " +
                        "on this invoice line.");
                }

                var serial =
                    requestedItem.SerialNumber?.Trim();

                if (!string.IsNullOrWhiteSpace(serial))
                {
                    var duplicateSerial =
                        previousForLine.Any(x =>
                            string.Equals(
                                x.SerialNumber?.Trim(),
                                serial,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            ConsumesClaimQuantity(x));

                    if (duplicateSerial)
                    {
                        throw new InvalidOperationException(
                            $"Serial number '{serial}' already " +
                            "has an active or permanently " +
                            "consumed claim.");
                    }

                    // One recorded serial represents
                    // one physical unit.
                    if (requestedItem.Quantity != 1)
                    {
                        throw new InvalidOperationException(
                            "A claim with one serial number " +
                            "must have a quantity of 1.");
                    }
                }

                claim.Items.Add(
                    new WarrantyClaimItem
                    {
                        SaleLineId =
                            saleLine.Id,

                        Quantity =
                            requestedItem.Quantity,

                        SerialNumber =
                            string.IsNullOrWhiteSpace(serial)
                                ? null
                                : serial,

                        ReportedDefect =
                            requestedItem
                                .ReportedDefect?
                                .Trim(),

                        OriginalWarrantyEndDate =
                            saleLine.WarrantyEndDate,

                        ResolutionType =
                            WarrantyResolutionType.Pending
                    });
            }

            // W6.1 - The initial audit event is created
            // as part of the WarrantyClaim aggregate.
            //
            // This means the claim, claim items and initial
            // timeline event are persisted atomically by the
            // same SaveChanges and database transaction.
            claim.Events.Add(
                new WarrantyClaimEvent
                {
                    EventType =
                        WarrantyClaimEventType.ClaimSubmitted,

                    FromStatus = null,

                    ToStatus =
                        WarrantyClaimStatus.Submitted,

                    Description =
                        "Warranty claim submitted.",

                    Notes =
                        claim.CustomerComplaint
                });

            _dbContext.WarrantyClaims.Add(claim);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new RegisterWarrantyClaimResultDto
            {
                WarrantyClaimId =
                    claim.Id,

                ClaimNumber =
                    claim.ClaimNumber,

                ItemCount =
                    claim.Items.Count,

                TotalQuantity =
                    claim.Items.Sum(x => x.Quantity),

                ClaimDate =
                    claim.ClaimDate
            };
        }
        catch (PostgresException ex)
            when (ex.SqlState is "40001" or "40P01")
        {
            // 40001: serialization failure
            // 40P01: deadlock detected
            throw new InvalidOperationException(
                "Another transaction changed the warranty claim " +
                "information. Please retry the registration.",
                ex);
        }
    }

    private static void ValidateWarranty(
        SaleLine saleLine,
        DateTime today)
    {
        if (!saleLine.WarrantyMonths.HasValue ||
            saleLine.WarrantyMonths.Value <= 0 ||
            !saleLine.WarrantyStartDate.HasValue ||
            !saleLine.WarrantyEndDate.HasValue)
        {
            throw new InvalidOperationException(
                "This product has no recorded " +
                "customer warranty.");
        }

        if (today <
            saleLine.WarrantyStartDate.Value.Date)
        {
            throw new InvalidOperationException(
                "This product's warranty has not started.");
        }

        if (today >
            saleLine.WarrantyEndDate.Value.Date)
        {
            throw new InvalidOperationException(
                "This product's warranty has expired.");
        }
    }

    private static bool ConsumesClaimQuantity(
        WarrantyClaimItem item)
    {
        var claim = item.WarrantyClaim;

        if (claim is null)
            return false;

        return claim.Status switch
        {
            WarrantyClaimStatus.Submitted =>
                true,

            WarrantyClaimStatus.UnderInspection =>
                true,

            WarrantyClaimStatus.Approved =>
                true,

            WarrantyClaimStatus.InRepair =>
                true,

            WarrantyClaimStatus.AwaitingReplacement =>
                true,

            WarrantyClaimStatus.Resolved =>
                item.ResolutionType is
                    WarrantyResolutionType.Replaced or
                    WarrantyResolutionType.Refunded,

            _ => false
        };
    }

    private static bool HasMoreThanThreeDecimalPlaces(
    decimal value)
    {
        return decimal.Round(
            value,
            3,
            MidpointRounding.ToZero) != value;
    }
}