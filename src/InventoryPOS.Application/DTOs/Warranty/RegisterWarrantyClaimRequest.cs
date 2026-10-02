
namespace InventoryPOS.Application.DTOs.Warranty;

public sealed class RegisterWarrantyClaimRequest
{
    public Guid SaleId { get; set; }

    public string? CustomerComplaint { get; set; }

    public List<RegisterWarrantyClaimItemRequest> Items { get; set; }
        = new();
}
