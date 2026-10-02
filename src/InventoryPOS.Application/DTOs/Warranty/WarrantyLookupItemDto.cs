
namespace InventoryPOS.Application.DTOs.Warranty;

public class WarrantyLookupItemDto
{
    public Guid SaleLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public decimal QuantityPurchased { get; set; }

    public int? WarrantyMonths { get; set; }

    public DateTime? WarrantyStartDate { get; set; }

    public DateTime? WarrantyEndDate { get; set; }

    public WarrantyEligibilityStatus EligibilityStatus { get; set; }

    public string EligibilityMessage { get; set; } = string.Empty;
    public decimal RemainingClaimableQuantity { get; set; }
}
