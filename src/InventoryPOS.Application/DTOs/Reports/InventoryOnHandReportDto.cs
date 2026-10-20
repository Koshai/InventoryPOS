namespace InventoryPOS.Application.DTOs.Reports;

public sealed class InventoryOnHandReportDto
{
    public Guid? LocationId { get; set; }

    public int SkuCount { get; set; }

    public int LowStockCount { get; set; }

    public int NearExpiryBatchCount { get; set; }

    public decimal OnHandQuantity { get; set; }

    public decimal InventoryValueAtCost { get; set; }

    public IReadOnlyList<InventoryOnHandLineDto> Lines { get; set; }
        = Array.Empty<InventoryOnHandLineDto>();
}

public sealed class InventoryOnHandLineDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsLowStock { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineValueAtCost { get; set; }
}
