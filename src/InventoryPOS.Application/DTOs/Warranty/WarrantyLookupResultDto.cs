
namespace InventoryPOS.Application.DTOs.Warranty;

public class WarrantyLookupResultDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public List<WarrantyLookupItemDto> Items { get; set; } = new();
}
