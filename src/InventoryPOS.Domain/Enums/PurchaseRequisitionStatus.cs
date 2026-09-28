namespace InventoryPOS.Domain.Enums;

public enum PurchaseRequisitionStatus
{
    Draft = 0,
    Sent = 1,
    Approved = 2,
    ConvertedToPurchaseOrder = 3,
    Cancelled = 4
}