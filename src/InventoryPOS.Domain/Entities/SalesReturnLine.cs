using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class SalesReturnLine : BaseEntity
{
    public Guid SalesReturnId { get; set; }

    public Guid SaleLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsRestockable { get; set; } = true;

    public string? Reason { get; set; }

    public SalesReturn? SalesReturn { get; set; }

    public SaleLine? SaleLine { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}