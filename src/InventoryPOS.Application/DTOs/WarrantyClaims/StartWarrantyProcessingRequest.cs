
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class StartWarrantyProcessingRequest
{
    public Guid ClaimId { get; set; }

    public string? ProcessingNotes { get; set; }
}
