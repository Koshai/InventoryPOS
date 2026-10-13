using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Products;

public sealed class ProductBarcodeDto
{
    public Guid Id { get; init; }

    public Guid ProductVariantId { get; init; }

    public string Value { get; init; }
        = string.Empty;

    public BarcodeType Type { get; init; }

    public bool IsPrimary { get; init; }
}