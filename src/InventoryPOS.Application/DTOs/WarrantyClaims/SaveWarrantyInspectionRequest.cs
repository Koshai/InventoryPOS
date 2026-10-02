
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class SaveWarrantyInspectionRequest
{
    public Guid ClaimId { get; set; }

    public string InspectionNotes { get; set; } = string.Empty;

    // Optional notes for each claimed product.
    public List<WarrantyTechnicianNoteRequest> ItemNotes { get; set; }
        = new();
}

public sealed class WarrantyTechnicianNoteRequest
{
    public Guid ClaimItemId { get; set; }

    public string TechnicianNotes { get; set; } = string.Empty;
}
