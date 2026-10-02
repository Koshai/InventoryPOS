
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class DecideWarrantyClaimRequest
{
    public Guid ClaimId { get; set; }

    // Required for rejection; optional for approval.
    public string? DecisionNotes { get; set; }
}
