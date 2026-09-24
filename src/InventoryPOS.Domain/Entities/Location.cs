using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string LocationType { get; set; } = "Store";

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}