namespace InventoryPOS.Application.DTOs.Products;

public sealed class ProductBarcodeRenderDto
{
    public Guid ProductVariantId { get; init; }

    public string Value { get; init; } = string.Empty;

    public string Format { get; init; } = string.Empty;

    public byte[] ImageBytes { get; init; } = [];
}