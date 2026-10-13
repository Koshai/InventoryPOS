using InventoryPOS.Domain.Common;

namespace InventoryPOS.Domain.Entities;

public class DocumentNumberCounter : BaseEntity
{
    public string DocumentPrefix { get; set; }
        = string.Empty;

    public DateOnly DocumentDate { get; set; }

    public int LastNumber { get; set; }
}