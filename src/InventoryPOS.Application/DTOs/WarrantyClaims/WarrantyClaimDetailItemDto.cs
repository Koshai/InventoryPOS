
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class WarrantyClaimDetailItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? SerialNumber { get; set; }
    public string? ReportedDefect { get; set; }
    public string? TechnicianNotes { get; set; }
    public string ResolutionType { get; set; } = string.Empty;
    public DateTime? OriginalWarrantyEndDate { get; set; }

    public string QuantityDisplay => Quantity.ToString("0.###");

    public string WarrantyEndDisplay =>
        OriginalWarrantyEndDate?.ToString("dd MMM yyyy") ?? "—";
}
