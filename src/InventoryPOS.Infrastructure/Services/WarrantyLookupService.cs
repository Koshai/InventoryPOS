
using InventoryPOS.Application.DTOs.Warranty;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class WarrantyLookupService : IWarrantyLookupService
{
    private readonly InventoryPosDbContext _dbContext;

    public WarrantyLookupService(InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarrantyInvoiceSearchPageDto> SearchInvoicesAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Sales
            .AsNoTracking()
            .AsQueryable();

        var term = searchText?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
        {
            // Escape LIKE wildcards so a customer's search is literal.
            var escaped = term
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            var pattern = $"%{escaped}%";

            query = query.Where(s =>
                EF.Functions.ILike(
                    s.InvoiceNumber, pattern, "\\")
                || (s.Customer != null &&
                    (EF.Functions.ILike(
                        s.Customer.Name, pattern, "\\")
                     || (s.Customer.Phone != null &&
                         EF.Functions.ILike(
                             s.Customer.Phone, pattern, "\\"))))
                || s.Lines.Any(line =>
                    line.ProductVariant != null &&
                    (EF.Functions.ILike(
                        line.ProductVariant.SKU, pattern, "\\")
                     || (line.ProductVariant.Product != null &&
                         EF.Functions.ILike(
                             line.ProductVariant.Product.Name,
                             pattern, "\\")))));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new WarrantyInvoiceSearchDto
            {
                SaleId = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                SaleDate = s.SaleDate,
                CustomerName = s.Customer != null
                    ? s.Customer.Name
                    : "Walk-in Customer",
                CustomerPhone = s.Customer != null
                    ? s.Customer.Phone
                    : null,
                TotalAmount = s.TotalAmount,
                ProductLineCount = s.Lines.Count
            })
            .ToListAsync(cancellationToken);

        var saleIds = sales.Select(s => s.SaleId).ToArray();

        if (saleIds.Length > 0)
        {
            var products = await _dbContext.SaleLines
                .AsNoTracking()
                .Where(line => saleIds.Contains(line.SaleId))
                .Select(line => new
                {
                    line.SaleId,
                    ProductName =
                        line.ProductVariant != null &&
                        line.ProductVariant.Product != null
                            ? line.ProductVariant.Product.Name
                            : "Unknown product"
                })
                .ToListAsync(cancellationToken);

            var summaries = products
                .GroupBy(x => x.SaleId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(
                        ", ",
                        g.Select(x => x.ProductName)
                            .Distinct()
                            .Take(3)));

            foreach (var sale in sales)
            {
                sale.ProductSummary =
                    summaries.GetValueOrDefault(sale.SaleId)
                    ?? "No products";
            }
        }

        return new WarrantyInvoiceSearchPageDto
        {
            Items = sales,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<WarrantyLookupResultDto?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new ArgumentException(
                "Please enter an invoice number.",
                nameof(invoiceNumber));
        }

        var saleId = await _dbContext.Sales
            .AsNoTracking()
            .Where(s => s.InvoiceNumber == invoiceNumber.Trim())
            .Select(s => (Guid?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return saleId.HasValue
            ? await GetBySaleIdAsync(
                saleId.Value, cancellationToken)
            : null;
    }

    public async Task<WarrantyLookupResultDto?> GetBySaleIdAsync(
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var sale = await _dbContext.Sales
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Customer)
            .Include(s => s.Lines)
                .ThenInclude(line => line.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
            .SingleOrDefaultAsync(
                s => s.Id == saleId,
                cancellationToken);

        if (sale is null)
            return null;

        var today = DateTime.UtcNow.Date;

        var result = new WarrantyLookupResultDto
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate = sale.SaleDate,
            CustomerId = sale.CustomerId,
            CustomerName =
                sale.Customer?.Name ?? "Walk-in Customer",
            CustomerPhone = sale.Customer?.Phone
        };

        foreach (var line in sale.Lines.OrderBy(
                     x => x.ProductVariant?.SKU))
        {
            var status = DetermineEligibility(
                line.WarrantyMonths,
                line.WarrantyStartDate,
                line.WarrantyEndDate,
                today);

            result.Items.Add(new WarrantyLookupItemDto
            {
                SaleLineId = line.Id,
                ProductVariantId = line.ProductVariantId,
                ProductName =
                    line.ProductVariant?.Product?.Name
                    ?? "Unknown product",
                SKU = line.ProductVariant?.SKU ?? string.Empty,
                QuantityPurchased = line.Quantity,
                WarrantyMonths = line.WarrantyMonths,
                WarrantyStartDate = line.WarrantyStartDate,
                WarrantyEndDate = line.WarrantyEndDate,
                EligibilityStatus = status,
                EligibilityMessage = GetEligibilityMessage(
                    status, line.WarrantyEndDate)
            });
        }

        await PopulateRemainingQuantitiesAsync(result,cancellationToken);

        return result;
    }

    private static WarrantyEligibilityStatus DetermineEligibility(
        int? warrantyMonths,
        DateTime? startDate,
        DateTime? endDate,
        DateTime today)
    {
        if (!warrantyMonths.HasValue ||
            warrantyMonths.Value <= 0 ||
            !startDate.HasValue ||
            !endDate.HasValue)
        {
            return WarrantyEligibilityStatus.NoWarranty;
        }

        if (today < startDate.Value.Date)
            return WarrantyEligibilityStatus.NotStarted;

        if (today > endDate.Value.Date)
            return WarrantyEligibilityStatus.Expired;

        return WarrantyEligibilityStatus.Active;
    }

    private static string GetEligibilityMessage(
        WarrantyEligibilityStatus status,
        DateTime? endDate)
    {
        return status switch
        {
            WarrantyEligibilityStatus.Active =>
                $"Warranty active until {endDate:dd MMM yyyy}",

            WarrantyEligibilityStatus.Expired =>
                $"Warranty expired on {endDate:dd MMM yyyy}",

            WarrantyEligibilityStatus.NotStarted =>
                "Warranty has not started yet.",

            _ => "No warranty recorded for this item."
        };
    }


    private async Task PopulateRemainingQuantitiesAsync(
        WarrantyLookupResultDto result,
        CancellationToken cancellationToken)
    {
        var saleLineIds = result.Items
            .Select(x => x.SaleLineId)
            .ToArray();

        if (saleLineIds.Length == 0)
            return;

        var previousClaims = await _dbContext.WarrantyClaimItems
            .AsNoTracking()
            .Where(x => saleLineIds.Contains(x.SaleLineId))
            .Select(x => new
            {
                x.SaleLineId,
                x.Quantity,
                x.ResolutionType,
                ClaimStatus = x.WarrantyClaim!.Status
            })
            .ToListAsync(cancellationToken);

        foreach (var item in result.Items)
        {
            var occupiedQuantity = previousClaims
                .Where(x => x.SaleLineId == item.SaleLineId)
                .Where(x =>
                    x.ClaimStatus == WarrantyClaimStatus.Submitted ||
                    x.ClaimStatus == WarrantyClaimStatus.UnderInspection ||
                    x.ClaimStatus == WarrantyClaimStatus.Approved ||
                    x.ClaimStatus == WarrantyClaimStatus.InRepair ||
                    x.ClaimStatus == WarrantyClaimStatus.AwaitingReplacement ||
                    (x.ClaimStatus == WarrantyClaimStatus.Resolved &&
                     (x.ResolutionType == WarrantyResolutionType.Replaced ||
                      x.ResolutionType == WarrantyResolutionType.Refunded)))
                .Sum(x => x.Quantity);

            item.RemainingClaimableQuantity =
                Math.Max(0, item.QuantityPurchased - occupiedQuantity);
        }
    }
}
