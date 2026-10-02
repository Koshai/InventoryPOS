using InventoryPOS.Application.DTOs.WarrantyClaims;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class WarrantyClaimQueryService
    : IWarrantyClaimQueryService
{
    private readonly InventoryPosDbContext _db;

    public WarrantyClaimQueryService(
        InventoryPosDbContext db)
    {
        _db = db;
    }

    public async Task<WarrantyClaimPageDto> SearchAsync(
        string? searchText,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.WarrantyClaims
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            // Escape SQL LIKE wildcards so search text
            // is treated literally.
            var escaped = searchText.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            var pattern = $"%{escaped}%";

            query = query.Where(c =>
                EF.Functions.ILike(
                    c.ClaimNumber,
                    pattern,
                    "\\") ||

                EF.Functions.ILike(
                    c.Sale!.InvoiceNumber,
                    pattern,
                    "\\") ||

                (c.Customer != null &&
                    (EF.Functions.ILike(
                        c.Customer.Name,
                        pattern,
                        "\\") ||

                     EF.Functions.ILike(
                        c.Customer.Phone,
                        pattern,
                        "\\"))));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        if (totalPages > 0)
        {
            page = Math.Min(
                page,
                totalPages);
        }

        var claims = await query
            .OrderByDescending(c => c.ClaimDate)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c =>
                new WarrantyClaimListItemDto
                {
                    Id = c.Id,

                    ClaimNumber =
                        c.ClaimNumber,

                    ClaimDate =
                        c.ClaimDate,

                    InvoiceNumber =
                        c.Sale!.InvoiceNumber,

                    CustomerName =
                        c.Customer != null
                            ? c.Customer.Name
                            : "Walk-in customer",

                    Status =
                        c.Status.ToString(),

                    ProductLineCount =
                        c.Items.Count
                })
            .ToListAsync(cancellationToken);

        return new WarrantyClaimPageDto
        {
            Items = claims,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<WarrantyClaimDetailsDto?> GetDetailsAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        var claim = await _db.WarrantyClaims
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Sale)
            .Include(c => c.Customer)
            .Include(c => c.Items)
                .ThenInclude(i => i.SaleLine)
                    .ThenInclude(l => l!.ProductVariant)
                        .ThenInclude(v => v!.Product)
            .SingleOrDefaultAsync(
                c => c.Id == claimId,
                cancellationToken);

        if (claim is null)
        {
            return null;
        }

        // W5 - Replacement history.
        var replacements =
            await _db.WarrantyReplacements
                .AsNoTracking()
                .Where(r =>
                    r.WarrantyClaimItem != null &&
                    r.WarrantyClaimItem.WarrantyClaimId ==
                        claimId)
                .OrderBy(r => r.IssuedAtUtc)
                .ThenBy(r => r.CreatedAtUtc)
                .ThenBy(r => r.Id)
                .Select(r =>
                    new WarrantyReplacementHistoryDto
                    {
                        Id = r.Id,

                        WarrantyClaimItemId =
                            r.WarrantyClaimItemId,

                        ReplacementVariantId =
                            r.ReplacementVariantId,

                        ProductName =
                            r.ReplacementVariant != null &&
                            r.ReplacementVariant.Product != null
                                ? r.ReplacementVariant
                                    .Product.Name
                                : "Unknown product",

                        SKU =
                            r.ReplacementVariant != null
                                ? r.ReplacementVariant.SKU
                                : string.Empty,

                        Quantity =
                            r.Quantity,

                        LocationName =
                            r.Location != null
                                ? r.Location.Name
                                : string.Empty,

                        SupplierBatchNumber =
                            r.InventoryBatch != null
                                ? r.InventoryBatch
                                    .SupplierBatchNumber
                                : null,

                        IssuedAtUtc =
                            r.IssuedAtUtc,

                        Notes =
                            r.Notes
                    })
                .ToListAsync(cancellationToken);

        // W6.1 - Persisted warranty lifecycle timeline.
        //
        // We query the audit table independently rather than
        // including it in the large claim graph. This keeps
        // the details query predictable and avoids multiplying
        // claim-item rows against event rows.
        var events =
            await _db.WarrantyClaimEvents
                .AsNoTracking()
                .Where(e =>
                    e.WarrantyClaimId == claimId)
                .OrderBy(e => e.CreatedAtUtc)
                .ThenBy(e => e.Id)
                .Select(e =>
                    new WarrantyClaimEventDto
                    {
                        Id = e.Id,

                        EventType =
                            e.EventType.ToString(),

                        Description =
                            e.Description,

                        FromStatus =
                            e.FromStatus.HasValue
                                ? e.FromStatus.Value.ToString()
                                : null,

                        ToStatus =
                            e.ToStatus.HasValue
                                ? e.ToStatus.Value.ToString()
                                : null,

                        Notes =
                            e.Notes,

                        CreatedAtUtc =
                            e.CreatedAtUtc
                    })
                .ToListAsync(cancellationToken);

        return new WarrantyClaimDetailsDto
        {
            Id = claim.Id,

            ClaimNumber =
                claim.ClaimNumber,

            ClaimDate =
                claim.ClaimDate,

            Status =
                claim.Status.ToString(),

            InvoiceNumber =
                claim.Sale?.InvoiceNumber
                ?? string.Empty,

            CustomerName =
                claim.Customer?.Name
                ?? "Walk-in customer",

            CustomerPhone =
                claim.Customer?.Phone,

            CustomerComplaint =
                claim.CustomerComplaint,

            InspectionNotes =
                claim.InspectionNotes,

            ResolutionNotes =
                claim.ResolutionNotes,

            ResolvedAtUtc =
                claim.ResolvedAtUtc,

            Items = claim.Items
                .Select(i =>
                    new WarrantyClaimDetailItemDto
                    {
                        Id =
                            i.Id,

                        ProductName =
                            i.SaleLine?
                                .ProductVariant?
                                .Product?
                                .Name
                            ?? "Unknown product",

                        SKU =
                            i.SaleLine?
                                .ProductVariant?
                                .SKU
                            ?? string.Empty,

                        Quantity =
                            i.Quantity,

                        SerialNumber =
                            i.SerialNumber,

                        ReportedDefect =
                            i.ReportedDefect,

                        TechnicianNotes =
                            i.TechnicianNotes,

                        ResolutionType =
                            i.ResolutionType.ToString(),

                        OriginalWarrantyEndDate =
                            i.OriginalWarrantyEndDate
                    })
                .ToList(),

            Replacements =
                replacements,

            Events =
                events
        };
    }
}