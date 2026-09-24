using InventoryPOS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryPOS.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? NameBn { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public Product? Product { get; set; }
}
