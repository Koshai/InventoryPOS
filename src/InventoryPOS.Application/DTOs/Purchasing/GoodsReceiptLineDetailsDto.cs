namespace InventoryPOS.Application.DTOs.Purchasing;

public class GoodsReceiptLineDetailsDto
{
    public Guid Id { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal ReceivedQuantity { get; set; }

    public decimal UnitCost { get; set; }

    public string SupplierBatchNumber { get; set; } = string.Empty;

    public DateTime? ManufactureDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int? WarrantyMonths { get; set; }

    public decimal LineTotal =>
        ReceivedQuantity * UnitCost;

    public string FormattedLineTotal =>
        $"৳{LineTotal:N2}";

    public string ManufactureDateDisplay =>
        ManufactureDate.HasValue
            ? ManufactureDate.Value.ToLocalTime().ToString("MMM dd, yyyy")
            : "—";

    public string ExpiryDateDisplay =>
        ExpiryDate.HasValue
            ? ExpiryDate.Value.ToLocalTime().ToString("MMM dd, yyyy")
            : "—";

    public string WarrantyDisplay =>
        WarrantyMonths.HasValue && WarrantyMonths > 0
            ? $"{WarrantyMonths} months"
            : "—";
}