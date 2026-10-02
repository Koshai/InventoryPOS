using System.Data;
using InventoryPOS.Application.DTOs.WarrantyClaims;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class WarrantyClaimManagementService
    : IWarrantyClaimManagementService
{
    private readonly InventoryPosDbContext _db;

    public WarrantyClaimManagementService(
        InventoryPosDbContext db)
    {
        _db = db;
    }

    public async Task StartInspectionAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        RequireClaimId(claimId);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var changed = await _db.WarrantyClaims
            .Where(c =>
                c.Id == claimId &&
                c.Status == WarrantyClaimStatus.Submitted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    c => c.Status,
                    WarrantyClaimStatus.UnderInspection),
                cancellationToken);

        if (changed != 1)
        {
            throw new InvalidOperationException(
                "The claim was not found or is no longer awaiting " +
                "inspection. Refresh the claim and try again.");
        }

        AddClaimEvent(
            claimId,
            WarrantyClaimEventType.InspectionStarted,
            "Warranty claim inspection started.",
            WarrantyClaimStatus.Submitted,
            WarrantyClaimStatus.UnderInspection);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveInspectionAsync(
        SaveWarrantyInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireClaimId(request.ClaimId);

        var notes = RequireText(
            request.InspectionNotes,
            "Inspection notes",
            2000);

        var itemNotes = request.ItemNotes
            ?? new List<WarrantyTechnicianNoteRequest>();

        if (itemNotes.Any(i => i.ClaimItemId == Guid.Empty) ||
            itemNotes.GroupBy(i => i.ClaimItemId)
                .Any(g => g.Count() > 1))
        {
            throw new InvalidOperationException(
                "Inspection items must be unique and have valid IDs.");
        }

        foreach (var item in itemNotes)
        {
            if (item.TechnicianNotes?.Length > 2000)
            {
                throw new InvalidOperationException(
                    "Technician notes cannot exceed 2000 characters.");
            }
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var changed = await _db.WarrantyClaims
            .Where(c =>
                c.Id == request.ClaimId &&
                c.Status == WarrantyClaimStatus.UnderInspection)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    c => c.InspectionNotes,
                    notes),
                cancellationToken);

        if (changed != 1)
        {
            throw new InvalidOperationException(
                "The claim is not under inspection. " +
                "Refresh its details.");
        }

        if (itemNotes.Count > 0)
        {
            var itemIds = itemNotes
                .Select(i => i.ClaimItemId)
                .ToArray();

            var existingIds =
                await _db.WarrantyClaimItems
                    .AsNoTracking()
                    .Where(i =>
                        i.WarrantyClaimId == request.ClaimId &&
                        itemIds.Contains(i.Id))
                    .Select(i => i.Id)
                    .ToListAsync(cancellationToken);

            if (existingIds.Count != itemIds.Length)
            {
                throw new InvalidOperationException(
                    "One or more inspection items do not " +
                    "belong to this claim.");
            }

            foreach (var item in itemNotes)
            {
                var technicianNotes =
                    item.TechnicianNotes?.Trim();

                await _db.WarrantyClaimItems
                    .Where(i =>
                        i.Id == item.ClaimItemId &&
                        i.WarrantyClaimId == request.ClaimId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            i => i.TechnicianNotes,
                            technicianNotes),
                        cancellationToken);
            }
        }

        AddClaimEvent(
            request.ClaimId,
            WarrantyClaimEventType.InspectionUpdated,
            "Warranty inspection findings updated.",
            WarrantyClaimStatus.UnderInspection,
            WarrantyClaimStatus.UnderInspection,
            notes);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ApproveAsync(
        DecideWarrantyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireClaimId(request.ClaimId);

        var decisionNotes = OptionalText(
            request.DecisionNotes,
            "Decision notes",
            2000);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var changed = await _db.WarrantyClaims
            .Where(c =>
                c.Id == request.ClaimId &&
                c.Status == WarrantyClaimStatus.UnderInspection &&
                c.InspectionNotes != null &&
                c.InspectionNotes.Trim() != "")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        c => c.Status,
                        WarrantyClaimStatus.Approved)
                    .SetProperty(
                        c => c.ResolutionNotes,
                        decisionNotes),
                cancellationToken);

        if (changed != 1)
        {
            throw new InvalidOperationException(
                "The claim must be under inspection with saved " +
                "inspection notes before it can be approved. " +
                "Refresh its details.");
        }

        AddClaimEvent(
            request.ClaimId,
            WarrantyClaimEventType.ClaimApproved,
            "Warranty claim approved.",
            WarrantyClaimStatus.UnderInspection,
            WarrantyClaimStatus.Approved,
            decisionNotes);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RejectAsync(
        DecideWarrantyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireClaimId(request.ClaimId);

        var reason = RequireText(
            request.DecisionNotes,
            "Rejection reason",
            2000);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var changed = await _db.WarrantyClaims
            .Where(c =>
                c.Id == request.ClaimId &&
                c.Status == WarrantyClaimStatus.UnderInspection &&
                c.InspectionNotes != null &&
                c.InspectionNotes.Trim() != "")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        c => c.Status,
                        WarrantyClaimStatus.Rejected)
                    .SetProperty(
                        c => c.ResolutionNotes,
                        reason),
                cancellationToken);

        if (changed != 1)
        {
            throw new InvalidOperationException(
                "The claim must be under inspection with saved " +
                "inspection notes before it can be rejected. " +
                "Refresh its details.");
        }

        AddClaimEvent(
            request.ClaimId,
            WarrantyClaimEventType.ClaimRejected,
            "Warranty claim rejected.",
            WarrantyClaimStatus.UnderInspection,
            WarrantyClaimStatus.Rejected,
            reason);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task StartRepairAsync(
        StartWarrantyProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        return StartProcessingAsync(
            request,
            WarrantyClaimStatus.InRepair,
            cancellationToken);
    }

    public Task PrepareReplacementAsync(
        StartWarrantyProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        return StartProcessingAsync(
            request,
            WarrantyClaimStatus.AwaitingReplacement,
            cancellationToken);
    }

    private async Task StartProcessingAsync(
        StartWarrantyProcessingRequest request,
        WarrantyClaimStatus targetStatus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireClaimId(request.ClaimId);

        if (targetStatus != WarrantyClaimStatus.InRepair &&
            targetStatus != WarrantyClaimStatus.AwaitingReplacement)
        {
            throw new InvalidOperationException(
                "Invalid warranty processing status.");
        }

        var notes = OptionalText(
            request.ProcessingNotes,
            "Processing notes",
            2000);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var changed = await _db.WarrantyClaims
            .Where(c =>
                c.Id == request.ClaimId &&
                c.Status == WarrantyClaimStatus.Approved)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    c => c.Status,
                    targetStatus),
                cancellationToken);

        if (changed != 1)
        {
            throw new InvalidOperationException(
                "The claim is no longer awaiting processing. " +
                "Refresh its details.");
        }

        if (notes is not null)
        {
            var previousNotes = await _db.WarrantyClaims
                .AsNoTracking()
                .Where(c => c.Id == request.ClaimId)
                .Select(c => c.ResolutionNotes)
                .SingleAsync(cancellationToken);

            var combined =
                string.IsNullOrWhiteSpace(previousNotes)
                    ? notes
                    : $"{previousNotes}\n\nProcessing: {notes}";

            if (combined.Length > 2000)
            {
                throw new InvalidOperationException(
                    "Combined resolution notes exceed " +
                    "2000 characters.");
            }

            await _db.WarrantyClaims
                .Where(c => c.Id == request.ClaimId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        c => c.ResolutionNotes,
                        combined),
                    cancellationToken);
        }

        var eventType =
            targetStatus == WarrantyClaimStatus.InRepair
                ? WarrantyClaimEventType.RepairStarted
                : WarrantyClaimEventType.ReplacementPrepared;

        var description =
            targetStatus == WarrantyClaimStatus.InRepair
                ? "Warranty repair started."
                : "Warranty replacement prepared.";

        AddClaimEvent(
            request.ClaimId,
            eventType,
            description,
            WarrantyClaimStatus.Approved,
            targetStatus,
            notes);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveTechnicianNotesAsync(
        Guid claimId,
        Guid claimItemId,
        string technicianNotes,
        CancellationToken cancellationToken = default)
    {
        RequireClaimId(claimId);

        if (claimItemId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Select a valid claim item.");
        }

        var notes = RequireText(
            technicianNotes,
            "Technician notes",
            2000);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        await LockClaimInStatusAsync(
    claimId,
    WarrantyClaimStatus.InRepair,
    "Technician notes can only be updated " +
    "while a claim is in repair.",
    cancellationToken);

        var updated = await _db.WarrantyClaimItems
            .Where(i =>
                i.Id == claimItemId &&
                i.WarrantyClaimId == claimId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    i => i.TechnicianNotes,
                    notes),
                cancellationToken);

        if (updated != 1)
        {
            throw new InvalidOperationException(
                "The selected product does not belong " +
                "to this claim.");
        }

        AddClaimEvent(
            claimId,
            WarrantyClaimEventType.TechnicianNotesUpdated,
            "Technician notes updated.",
            WarrantyClaimStatus.InRepair,
            WarrantyClaimStatus.InRepair,
            notes);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CompleteRepairAsync(
        CompleteWarrantyRepairRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireClaimId(request.ClaimId);

        var completionNotes = RequireText(
            request.CompletionNotes,
            "Repair completion notes",
            2000);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        await LockClaimInStatusAsync(
    request.ClaimId,
    WarrantyClaimStatus.InRepair,
    "This claim is no longer in repair. " +
    "Refresh its details before continuing.",
    cancellationToken);

        var items = await _db.WarrantyClaimItems
            .AsNoTracking()
            .Where(i =>
                i.WarrantyClaimId == request.ClaimId)
            .Select(i => new
            {
                i.Id,
                i.TechnicianNotes,
                i.ResolutionType
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            throw new InvalidOperationException(
                "The claim has no products to repair.");
        }

        if (items.Any(i =>
            string.IsNullOrWhiteSpace(i.TechnicianNotes)))
        {
            throw new InvalidOperationException(
                "Record technician notes for every claimed " +
                "product before completing the repair.");
        }

        if (items.Any(i =>
            i.ResolutionType != WarrantyResolutionType.Pending))
        {
            throw new InvalidOperationException(
                "One or more claim items already have a " +
                "resolution. Refresh the claim.");
        }

        var existingNotes = await _db.WarrantyClaims
            .AsNoTracking()
            .Where(c => c.Id == request.ClaimId)
            .Select(c => c.ResolutionNotes)
            .SingleAsync(cancellationToken);

        var combinedNotes =
            string.IsNullOrWhiteSpace(existingNotes)
                ? completionNotes
                : $"{existingNotes}\n\nRepair completed: " +
                  completionNotes;

        if (combinedNotes.Length > 2000)
        {
            throw new InvalidOperationException(
                "Combined resolution notes exceed 2000 " +
                "characters. Shorten the completion notes.");
        }

        var itemIds =
            items.Select(i => i.Id).ToArray();

        var updatedItems = await _db.WarrantyClaimItems
            .Where(i =>
                i.WarrantyClaimId == request.ClaimId &&
                itemIds.Contains(i.Id) &&
                i.ResolutionType ==
                    WarrantyResolutionType.Pending)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    i => i.ResolutionType,
                    WarrantyResolutionType.Repaired),
                cancellationToken);

        if (updatedItems != items.Count)
        {
            throw new InvalidOperationException(
                "Claim items changed during repair completion. " +
                "Refresh the claim.");
        }

        var completedAt = DateTime.UtcNow;

        var completed = await _db.WarrantyClaims
            .Where(c =>
                c.Id == request.ClaimId &&
                c.Status == WarrantyClaimStatus.InRepair)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        c => c.Status,
                        WarrantyClaimStatus.Resolved)
                    .SetProperty(
                        c => c.ResolutionNotes,
                        combinedNotes)
                    .SetProperty(
                        c => c.ResolvedAtUtc,
                        completedAt),
                cancellationToken);

        if (completed != 1)
        {
            throw new InvalidOperationException(
                "The claim could not be completed. " +
                "Refresh its details.");
        }

        AddClaimEvent(
            request.ClaimId,
            WarrantyClaimEventType.RepairCompleted,
            "Warranty repair completed.",
            WarrantyClaimStatus.InRepair,
            WarrantyClaimStatus.InRepair,
            completionNotes);

        AddClaimEvent(
            request.ClaimId,
            WarrantyClaimEventType.ClaimResolved,
            "Warranty claim resolved after repair.",
            WarrantyClaimStatus.InRepair,
            WarrantyClaimStatus.Resolved,
            completionNotes);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void AddClaimEvent(
        Guid claimId,
        WarrantyClaimEventType eventType,
        string description,
        WarrantyClaimStatus? fromStatus = null,
        WarrantyClaimStatus? toStatus = null,
        string? notes = null)
    {
        _db.WarrantyClaimEvents.Add(
            new WarrantyClaimEvent
            {
                WarrantyClaimId = claimId,
                EventType = eventType,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Description = description,
                Notes = notes
            });
    }

    private static void RequireClaimId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Select a valid warranty claim.");
        }
    }

    private static string RequireText(
        string? value,
        string field,
        int maximumLength)
    {
        var text = value?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"{field} is required.");
        }

        if (text.Length > maximumLength)
        {
            throw new InvalidOperationException(
                $"{field} cannot exceed " +
                $"{maximumLength} characters.");
        }

        return text;
    }

    private static string? OptionalText(
        string? value,
        string field,
        int maximumLength)
    {
        var text = value?.Trim();

        if (text?.Length > maximumLength)
        {
            throw new InvalidOperationException(
                $"{field} cannot exceed " +
                $"{maximumLength} characters.");
        }

        return string.IsNullOrWhiteSpace(text)
            ? null
            : text;
    }

    private async Task LockClaimInStatusAsync(
    Guid claimId,
    WarrantyClaimStatus requiredStatus,
    string errorMessage,
    CancellationToken cancellationToken)
    {
        var status = await _db.WarrantyClaims
            .FromSqlInterpolated($@"
            SELECT *
            FROM ""WarrantyClaims""
            WHERE ""Id"" = {claimId}
            FOR UPDATE")
            .AsNoTracking()
            .Where(c => c.Status == requiredStatus)
            .Select(c => (WarrantyClaimStatus?)c.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            throw new InvalidOperationException(
                errorMessage);
        }
    }
}