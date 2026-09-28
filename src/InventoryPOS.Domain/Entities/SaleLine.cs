using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class SaleLine : BaseEntity
{
    public Guid SaleId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }

    // ---------------------------------------------------------
    // Customer warranty snapshot
    // ---------------------------------------------------------

    public int? WarrantyMonths { get; set; }

    public DateTime? WarrantyStartDate { get; set; }

    public DateTime? WarrantyEndDate { get; set; }

    public Sale? Sale { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}