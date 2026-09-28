using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseReturnDetailsDto
{
    public Guid Id { get; set; }

    public string ReturnNumber { get; set; } = string.Empty;

    public DateTime ReturnDate { get; set; }

    public PurchaseReturnStatus Status { get; set; }

    public Guid GoodsReceiptId { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public string PurchaseOrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public List<PurchaseReturnDetailsLineDto> Lines { get; set; } = [];
}

public class PurchaseReturnDetailsLineDto
{
    public Guid Id { get; set; }

    public Guid GoodsReceiptLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid? InventoryBatchId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string SupplierBatchNumber { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }

    public string? Reason { get; set; }
    public string FormattedQuantity =>
    Quantity.ToString("0.###");

    public string FormattedUnitCost =>
        $"৳{UnitCost:N2}";

    public string FormattedLineTotal =>
        $"৳{LineTotal:N2}";
}