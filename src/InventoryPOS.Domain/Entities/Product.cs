using InventoryPOS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryPOS.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? NameBn { get; set; }

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public Guid UnitId { get; set; }

    public bool IsActive { get; set; } = true;

    public Category? Category { get; set; }

    public Brand? Brand { get; set; }

    public Unit? Unit { get; set; }

    public ICollection<ProductVariant> Variants { get; set; }
        = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; }
    = new List<ProductImage>();
}
