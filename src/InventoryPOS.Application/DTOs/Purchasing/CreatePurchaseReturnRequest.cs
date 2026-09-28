namespace InventoryPOS.Application.DTOs.Purchasing;

public class CreatePurchaseReturnRequest
{
    public Guid GoodsReceiptId { get; set; }

    public DateTime ReturnDate { get; set; }
        = DateTime.UtcNow;

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public List<CreatePurchaseReturnLineRequest> Lines { get; set; }
        = new();
}

public class CreatePurchaseReturnLineRequest
{
    public Guid GoodsReceiptLineId { get; set; }

    public Guid InventoryBatchId { get; set; }

    public decimal Quantity { get; set; }

    public string? Reason { get; set; }
}