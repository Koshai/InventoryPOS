using InventoryPOS.Application.DTOs.Reports;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class ReportQueryService : IReportQueryService
{
    private static readonly SaleStatus[] ActiveSaleStatuses =
    {
        SaleStatus.Completed,
        SaleStatus.PartiallyPaid,
        SaleStatus.Credit,
        SaleStatus.Refunded
    };

    private readonly InventoryPosDbContext _db;

    public ReportQueryService(InventoryPosDbContext db)
    {
        _db = db;
    }

    public async Task<SalesSummaryReportDto> GetSalesSummaryAsync(
        ReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValidateRange(filter);

        var rangeStart = filter.FromUtc.UtcDateTime;
        var rangeEnd = filter.ToUtc.UtcDateTime;

        var salesQuery = _db.Sales.AsNoTracking()
            .Where(s => ActiveSaleStatuses.Contains(s.Status))
            .Where(s => s.SaleDate >= rangeStart && s.SaleDate < rangeEnd);

        if (filter.LocationId.HasValue)
            salesQuery = salesQuery.Where(s => s.LocationId == filter.LocationId.Value);

        var salesAgg = await salesQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Gross = g.Sum(x => x.Subtotal),
                Discount = g.Sum(x => x.DiscountAmount),
                Tax = g.Sum(x => x.TaxAmount),
                Transport = g.Sum(x => x.TransportAmount),
                InvoiceTotal = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var returnsQuery = _db.SalesReturns.AsNoTracking()
            .Where(r => r.Status != SalesReturnStatus.Cancelled)
            .Where(r => r.ReturnDate >= rangeStart && r.ReturnDate < rangeEnd);

        if (filter.LocationId.HasValue)
            returnsQuery = returnsQuery.Where(r => r.LocationId == filter.LocationId.Value);

        var returnsAmount = await returnsQuery.SumAsync(r => (decimal?)r.RefundAmount, cancellationToken) ?? 0m;

        var paymentsQuery =
            from p in _db.SalePayments.AsNoTracking()
            join s in _db.Sales.AsNoTracking() on p.SaleId equals s.Id
            where p.PaymentDate >= rangeStart && p.PaymentDate < rangeEnd
            select new { p.Amount, s.LocationId };

        if (filter.LocationId.HasValue)
        {
            var locationId = filter.LocationId.Value;
            paymentsQuery = paymentsQuery.Where(x => x.LocationId == locationId);
        }

        var paymentsCollected = await paymentsQuery.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var count = salesAgg?.Count ?? 0;
        var invoiceTotal = salesAgg?.InvoiceTotal ?? 0m;
        var netSales = invoiceTotal - returnsAmount;

        return new SalesSummaryReportDto
        {
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            LocationId = filter.LocationId,
            TransactionCount = count,
            GrossSales = salesAgg?.Gross ?? 0m,
            DiscountAmount = salesAgg?.Discount ?? 0m,
            TaxAmount = salesAgg?.Tax ?? 0m,
            TransportAmount = salesAgg?.Transport ?? 0m,
            InvoiceTotal = invoiceTotal,
            SalesReturnsAmount = returnsAmount,
            NetSales = netSales,
            PaymentsCollected = paymentsCollected,
            AverageTransactionValue = count == 0 ? 0m : invoiceTotal / count
        };
    }

    public async Task<InventoryOnHandReportDto> GetInventoryOnHandAsync(
        Guid? locationId,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 2000);

        var query = _db.StockBalances.AsNoTracking()
            .Where(x =>
                x.ProductVariant != null &&
                x.ProductVariant.IsActive &&
                x.ProductVariant.Product != null &&
                x.ProductVariant.Product.IsActive);

        if (locationId.HasValue)
            query = query.Where(x => x.LocationId == locationId.Value);

        var lines = await query
            .OrderBy(x => x.ProductVariant!.Product!.Name)
            .ThenBy(x => x.ProductVariant!.SKU)
            .Take(take)
            .Select(x => new InventoryOnHandLineDto
            {
                ProductVariantId = x.ProductVariantId,
                Sku = x.ProductVariant!.SKU,
                ProductName = x.ProductVariant.Product!.Name,
                LocationId = x.LocationId,
                LocationName = x.Location != null ? x.Location.Name : string.Empty,
                QuantityOnHand = x.QuantityOnHand,
                QuantityAvailable = x.QuantityOnHand - x.QuantityReserved,
                ReorderLevel = x.ProductVariant.ReorderLevel,
                IsLowStock = (x.QuantityOnHand - x.QuantityReserved) <= x.ProductVariant.ReorderLevel,
                UnitCost = x.ProductVariant.CostPrice,
                LineValueAtCost = (x.QuantityOnHand - x.QuantityReserved) * x.ProductVariant.CostPrice
            })
            .ToListAsync(cancellationToken);

        var nearExpiryQuery = _db.InventoryBatches.AsNoTracking()
            .Where(b => b.QuantityOnHand > 0 && b.ExpiryDate != null)
            .Where(b => b.ExpiryDate!.Value <= DateTime.UtcNow.Date.AddDays(30));

        if (locationId.HasValue)
            nearExpiryQuery = nearExpiryQuery.Where(b => b.LocationId == locationId.Value);

        var nearExpiry = await nearExpiryQuery.CountAsync(cancellationToken);

        return new InventoryOnHandReportDto
        {
            LocationId = locationId,
            SkuCount = lines.Count,
            LowStockCount = lines.Count(x => x.IsLowStock),
            NearExpiryBatchCount = nearExpiry,
            OnHandQuantity = lines.Sum(x => x.QuantityOnHand),
            InventoryValueAtCost = lines.Sum(x => x.LineValueAtCost),
            Lines = lines
        };
    }

    public async Task<ReceivablesSummaryDto> GetReceivablesSummaryAsync(
        Guid? locationId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var asOf = asOfUtc.UtcDateTime;

        var dueQuery = _db.Sales.AsNoTracking()
            .Where(s => ActiveSaleStatuses.Contains(s.Status))
            .Where(s => s.DueAmount > 0);

        if (locationId.HasValue)
            dueQuery = dueQuery.Where(s => s.LocationId == locationId.Value);

        var rows = await dueQuery
            .Select(s => new { s.DueAmount, s.SaleDate })
            .ToListAsync(cancellationToken);

        var summary = new ReceivablesSummaryDto
        {
            OutstandingTotal = rows.Sum(x => x.DueAmount),
            InvoicesWithBalance = rows.Count
        };

        foreach (var row in rows)
        {
            var ageDays = (asOf.Date - row.SaleDate.Date).TotalDays;
            if (ageDays <= 0)
                summary.CurrentBucket += row.DueAmount;
            else if (ageDays <= 30)
                summary.Days1To30 += row.DueAmount;
            else if (ageDays <= 60)
                summary.Days31To60 += row.DueAmount;
            else if (ageDays <= 90)
                summary.Days61To90 += row.DueAmount;
            else
                summary.Days90Plus += row.DueAmount;
        }

        return summary;
    }

    public async Task<DashboardSnapshotDto> GetDashboardSnapshotAsync(
        ReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValidateRange(filter);

        var sales = await GetSalesSummaryAsync(filter, cancellationToken);
        var receivables = await GetReceivablesSummaryAsync(filter.LocationId, filter.ToUtc, cancellationToken);
        var inventory = await GetInventoryOnHandAsync(filter.LocationId, take: 500, cancellationToken);

        var rangeStart = filter.FromUtc.UtcDateTime;
        var rangeEnd = filter.ToUtc.UtcDateTime;

        var salesForDaily = _db.Sales.AsNoTracking()
            .Where(s => ActiveSaleStatuses.Contains(s.Status))
            .Where(s => s.SaleDate >= rangeStart && s.SaleDate < rangeEnd);

        if (filter.LocationId.HasValue)
            salesForDaily = salesForDaily.Where(s => s.LocationId == filter.LocationId.Value);

        var dailyRaw = await salesForDaily
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new
            {
                Day = g.Key,
                Net = g.Sum(x => x.TotalAmount),
                Count = g.Count()
            })
            .OrderBy(x => x.Day)
            .ToListAsync(cancellationToken);

        var topProductsQuery = _db.SaleLines.AsNoTracking()
            .Where(l => l.Sale != null &&
                        ActiveSaleStatuses.Contains(l.Sale.Status) &&
                        l.Sale.SaleDate >= rangeStart &&
                        l.Sale.SaleDate < rangeEnd);

        if (filter.LocationId.HasValue)
            topProductsQuery = topProductsQuery.Where(l => l.Sale!.LocationId == filter.LocationId.Value);

        var topProducts = await topProductsQuery
            .GroupBy(l => new
            {
                l.ProductVariantId,
                Sku = l.ProductVariant != null ? l.ProductVariant.SKU : string.Empty,
                Name = l.ProductVariant != null && l.ProductVariant.Product != null
                    ? l.ProductVariant.Product.Name
                    : string.Empty
            })
            .Select(g => new TopProductDto
            {
                ProductVariantId = g.Key.ProductVariantId,
                Sku = g.Key.Sku,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync(cancellationToken);

        var pendingPo = await _db.PurchaseOrders.AsNoTracking()
            .Where(p => p.Status == PurchaseOrderStatus.Ordered ||
                        p.Status == PurchaseOrderStatus.PartiallyReceived)
            .CountAsync(cancellationToken);

        var pendingPr = await _db.PurchaseRequisitions.AsNoTracking()
            .Where(p => p.Status == PurchaseRequisitionStatus.Draft ||
                        p.Status == PurchaseRequisitionStatus.Sent)
            .CountAsync(cancellationToken);

        var warrantyAction = await _db.WarrantyClaims.AsNoTracking()
            .Where(c =>
                c.Status == WarrantyClaimStatus.Submitted ||
                c.Status == WarrantyClaimStatus.UnderInspection ||
                c.Status == WarrantyClaimStatus.Approved ||
                c.Status == WarrantyClaimStatus.InRepair ||
                c.Status == WarrantyClaimStatus.AwaitingReplacement)
            .CountAsync(cancellationToken);

        return new DashboardSnapshotDto
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Filter = filter,
            Sales = sales,
            Receivables = receivables,
            LowStockSkuCount = inventory.LowStockCount,
            NearExpiryBatchCount = inventory.NearExpiryBatchCount,
            PendingPurchaseOrders = pendingPo,
            PendingPurchaseRequisitions = pendingPr,
            WarrantyClaimsRequiringAction = warrantyAction,
            GrossProfitAvailable = false,
            GrossProfit = null,
            GrossMarginPercent = null,
            DailySales = dailyRaw.Select(x => new DailySalesPointDto
            {
                BusinessDay = DateOnly.FromDateTime(x.Day),
                NetSales = x.Net,
                TransactionCount = x.Count
            }).ToList(),
            TopProductsByRevenue = topProducts
        };
    }

    private static void ValidateRange(ReportFilterDto filter)
    {
        if (filter.ToUtc <= filter.FromUtc)
            throw new ArgumentException("Report end must be after start.", nameof(filter));

        if (filter.ToUtc - filter.FromUtc > TimeSpan.FromDays(366))
            throw new ArgumentException("Report range cannot exceed 366 days.", nameof(filter));
    }
}
