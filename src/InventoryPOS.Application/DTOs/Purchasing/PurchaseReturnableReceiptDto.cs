namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseReturnableReceiptDto
{
    public Guid GoodsReceiptId { get; set; }

    public string ReceiptNumber { get; set; }
        = string.Empty;

    public DateTime ReceiptDate { get; set; }

    public Guid SupplierId { get; set; }

    public string SupplierName { get; set; }
        = string.Empty;

    public Guid LocationId { get; set; }

    public string LocationName { get; set; }
        = string.Empty;

    public string PurchaseOrderNumber { get; set; }
        = string.Empty;

    public List<PurchaseReturnableLineDto> Lines { get; set; }
        = new();
}

public class PurchaseReturnableLineDto
{
    public Guid GoodsReceiptLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid InventoryBatchId { get; set; }

    public string ProductName { get; set; }
        = string.Empty;

    public string SKU { get; set; }
        = string.Empty;

    public string SupplierBatchNumber { get; set; }
        = string.Empty;

    public decimal ReceivedQuantity { get; set; }

    public decimal PreviouslyReturnedQuantity { get; set; }

    public decimal MaximumDocumentReturnQuantity =>
        Math.Max(
            0,
            ReceivedQuantity -
            PreviouslyReturnedQuantity);

    public decimal BatchQuantityOnHand { get; set; }

    public decimal MaximumReturnQuantity =>
        Math.Min(
            MaximumDocumentReturnQuantity,
            BatchQuantityOnHand);

    public decimal UnitCost { get; set; }

    public decimal MaximumReturnValue =>
        MaximumReturnQuantity * UnitCost;

    public DateTime? ExpiryDate { get; set; }

    public int? WarrantyMonths { get; set; }
}