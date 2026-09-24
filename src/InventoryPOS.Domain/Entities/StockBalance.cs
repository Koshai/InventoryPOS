using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class StockBalance : BaseEntity
{
    public Guid ProductVariantId { get; set; }

    public Guid LocationId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public Location? Location { get; set; }

    public decimal QuantityAvailable =>
        QuantityOnHand - QuantityReserved;
}