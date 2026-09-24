namespace InventoryPOS.Domain.Enums;

public enum SaleStatus
{
    Draft = 0,
    Completed = 1,
    PartiallyPaid = 2,
    Credit = 3,
    Cancelled = 4,
    Refunded = 5
}