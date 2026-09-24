namespace InventoryPOS.Application.DTOs.Sales;

public class SaleResultDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}