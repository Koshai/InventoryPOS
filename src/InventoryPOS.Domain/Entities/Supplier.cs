using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class Supplier : BaseEntity
{
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public bool IsActive { get; set; } = true;
}