using InventoryPOS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryPOS.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? NameBn { get; set; }

    public bool IsActive { get; set; } = true;
}
