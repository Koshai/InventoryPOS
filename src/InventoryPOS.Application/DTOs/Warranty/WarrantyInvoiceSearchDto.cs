
namespace InventoryPOS.Application.DTOs.Warranty;

public sealed class WarrantyInvoiceSearchDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string ProductSummary { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int ProductLineCount { get; set; }
}
