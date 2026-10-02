using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Sales;

public class SalesQuoteHistoryDto
{
    public Guid Id { get; set; }

    public string QuoteNumber { get; set; }
        = string.Empty;

    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    public string CustomerName { get; set; }
        = string.Empty;

    public string LocationName { get; set; }
        = string.Empty;

    public SalesQuoteStatus Status { get; set; }

    public int LineCount { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public Guid? SaleId { get; set; }
}