using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class StockAdjustment : BaseEntity
{
    public Guid LocationId { get; set; }

    public StockAdjustmentStatus Status { get; set; }
        = StockAdjustmentStatus.Draft;

    public string Reason { get; set; }
        = string.Empty;

    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Location? Location { get; set; }

    public ICollection<StockAdjustmentLine> Lines { get; set; }
        = new List<StockAdjustmentLine>();
}