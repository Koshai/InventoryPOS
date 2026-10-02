namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class WarrantyReplacementHistoryDto
{
    public Guid Id { get; set; }

    public Guid WarrantyClaimItemId { get; set; }

    public Guid ReplacementVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public string? SupplierBatchNumber { get; set; }

    public DateTime IssuedAtUtc { get; set; }

    public string? Notes { get; set; }

    public string QuantityDisplay =>
        $"Qty: {Quantity:N3}";

    public string BatchDisplay =>
        string.IsNullOrWhiteSpace(SupplierBatchNumber)
            ? "Legacy / unbatched stock"
            : $"Batch: {SupplierBatchNumber}";

    public string IssuedAtDisplay =>
        IssuedAtUtc
            .ToLocalTime()
            .ToString("dd MMM yyyy, hh:mm tt");
}