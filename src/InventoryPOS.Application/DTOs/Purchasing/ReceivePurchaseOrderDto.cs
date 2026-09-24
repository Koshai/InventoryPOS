namespace InventoryPOS.Application.DTOs.Purchasing;

public class ReceivePurchaseOrderDto
{
    public Guid PurchaseOrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public IReadOnlyList<ReceivePurchaseOrderLineDto> Lines { get; set; }
        = Array.Empty<ReceivePurchaseOrderLineDto>();
}