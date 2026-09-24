using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class Customer : BaseEntity
{
    public string CustomerCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Sale> Sales { get; set; }
        = new List<Sale>();
}