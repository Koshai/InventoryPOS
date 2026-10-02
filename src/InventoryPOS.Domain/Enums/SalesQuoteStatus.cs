namespace InventoryPOS.Domain.Enums;

public enum SalesQuoteStatus
{
    Draft = 0,
    Issued = 1,
    Accepted = 2,
    ConvertedToSale = 3,
    Expired = 4,
    Cancelled = 5
}