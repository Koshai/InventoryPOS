namespace InventoryPOS.Application.DTOs.Sales;

public class SalesQuoteResultDto
{
    public Guid SalesQuoteId { get; set; }

    public string QuoteNumber { get; set; }
        = string.Empty;

    public int LineCount { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }
}