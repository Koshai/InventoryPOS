
using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class WarrantyClaimItem : BaseEntity
{
    public Guid WarrantyClaimId { get; set; }
    public WarrantyClaim WarrantyClaim { get; set; } = null!;

    public Guid SaleLineId { get; set; }
    public SaleLine SaleLine { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string? SerialNumber { get; set; }
    public string? ReportedDefect { get; set; }

    public DateTime? OriginalWarrantyEndDate { get; set; }

    public WarrantyResolutionType ResolutionType { get; set; } =
        WarrantyResolutionType.Pending;

    public string? TechnicianNotes { get; set; }
}
