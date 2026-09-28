using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public Guid LocationId { get; set; }

    public Guid? CustomerId { get; set; }

    public SaleStatus Status { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public string? Notes { get; set; }

    public Location? Location { get; set; }

    public Customer? Customer { get; set; }

    public ICollection<SaleLine> Lines { get; set; }
        = new List<SaleLine>();

    public ICollection<SalePayment> Payments { get; set; }
        = new List<SalePayment>();
    public ICollection<SalesReturn> Returns { get; set; }
    = new List<SalesReturn>();
}