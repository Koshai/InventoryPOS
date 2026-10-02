
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class WarrantyClaimListItemDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public DateTime ClaimDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProductLineCount { get; set; }

    public string ClaimDateDisplay =>
        ClaimDate.ToLocalTime().ToString("dd MMM yyyy");

    public string ProductCountDisplay =>
        $"{ProductLineCount} product line(s)";
}
