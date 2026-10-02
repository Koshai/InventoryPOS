
namespace InventoryPOS.Application.DTOs.Warranty;

public sealed class RegisterWarrantyClaimResultDto
{
    public Guid WarrantyClaimId { get; set; }

    public string ClaimNumber { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public decimal TotalQuantity { get; set; }

    public DateTime ClaimDate { get; set; }
}
