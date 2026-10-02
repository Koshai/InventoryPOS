
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class CompleteWarrantyRepairRequest
{
    public Guid ClaimId { get; set; }

    public string CompletionNotes { get; set; } = string.Empty;
}
