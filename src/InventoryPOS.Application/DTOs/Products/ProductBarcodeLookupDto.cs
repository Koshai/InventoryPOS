using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Products;

public sealed class ProductBarcodeLookupDto
{
    public Guid ProductVariantId { get; init; }

    public string Barcode { get; init; }
        = string.Empty;

    public BarcodeType BarcodeType { get; init; }
}