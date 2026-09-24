namespace InventoryPOS.Application.DTOs.Purchasing;

public class CreateGoodsReceiptRequest
{
    public Guid PurchaseOrderId { get; set; }

    public DateTime ReceiptDate { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyList<CreateGoodsReceiptLineRequest> Lines { get; set; }
        = Array.Empty<CreateGoodsReceiptLineRequest>();
}