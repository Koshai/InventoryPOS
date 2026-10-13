using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class ProductBarcode : BaseEntity
{
    public Guid ProductVariantId { get; set; }

    public string Value { get; set; }
        = string.Empty;

    public BarcodeType Type { get; set; }

    public bool IsPrimary { get; set; }

    public ProductVariant ProductVariant { get; set; }
        = null!;
}