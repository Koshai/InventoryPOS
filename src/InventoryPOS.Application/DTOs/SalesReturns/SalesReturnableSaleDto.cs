using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class SalesReturnableSaleDto
{
    public Guid SaleId { get; set; }

    public string InvoiceNumber { get; set; }
        = string.Empty;

    public DateTime SaleDate { get; set; }

    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; }
        = "Walk-in Customer";

    public SaleStatus SaleStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public List<SalesReturnableLineDto> Lines { get; set; }
        = new();

    public bool HasReturnableItems =>
        Lines.Any(x => x.ReturnableQuantity > 0);
}