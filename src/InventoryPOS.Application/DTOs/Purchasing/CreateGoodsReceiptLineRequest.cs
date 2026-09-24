namespace InventoryPOS.Application.DTOs.Purchasing;

public class CreateGoodsReceiptLineRequest
{
    public Guid PurchaseOrderLineId { get; set; }

    public decimal ReceivingQuantity { get; set; }

    public string SupplierBatchNumber { get; set; } = string.Empty;

    public DateTime? ManufactureDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int? WarrantyMonths { get; set; }
}