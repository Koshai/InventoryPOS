using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = true;

    public int SortOrder { get; set; }

    public Product? Product { get; set; }
}