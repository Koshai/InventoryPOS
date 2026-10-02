
namespace InventoryPOS.Application.DTOs.Warranty;

public sealed class RegisterWarrantyClaimItemRequest
{
    public Guid SaleLineId { get; set; }

    public decimal Quantity { get; set; }

    public string? SerialNumber { get; set; }

    public string? ReportedDefect { get; set; }
}
