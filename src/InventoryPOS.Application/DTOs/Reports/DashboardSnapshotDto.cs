namespace InventoryPOS.Application.DTOs.Reports;

public sealed class DashboardSnapshotDto
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public ReportFilterDto Filter { get; set; } = new();

    public SalesSummaryReportDto Sales { get; set; } = new();

    public ReceivablesSummaryDto Receivables { get; set; } = new();

    public int LowStockSkuCount { get; set; }

    public int NearExpiryBatchCount { get; set; }

    public int PendingPurchaseOrders { get; set; }

    public int PendingPurchaseRequisitions { get; set; }

    public int WarrantyClaimsRequiringAction { get; set; }

    /// <summary>
    /// False until sale-line cost snapshots / COGS method are implemented.
    /// </summary>
    public bool GrossProfitAvailable { get; set; }

    public decimal? GrossProfit { get; set; }

    public decimal? GrossMarginPercent { get; set; }

    public IReadOnlyList<DailySalesPointDto> DailySales { get; set; }
        = Array.Empty<DailySalesPointDto>();

    public IReadOnlyList<TopProductDto> TopProductsByRevenue { get; set; }
        = Array.Empty<TopProductDto>();
}

public sealed class DailySalesPointDto
{
    public DateOnly BusinessDay { get; set; }

    public decimal NetSales { get; set; }

    public int TransactionCount { get; set; }
}

public sealed class TopProductDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal QuantitySold { get; set; }

    public decimal Revenue { get; set; }
}
