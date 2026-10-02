
using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class WarrantyClaim : BaseEntity
{
    public string ClaimNumber { get; set; } = string.Empty;

    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public DateTime ClaimDate { get; set; } = DateTime.UtcNow;

    public WarrantyClaimStatus Status { get; set; } =
        WarrantyClaimStatus.Submitted;

    public string? CustomerComplaint { get; set; }
    public string? InspectionNotes { get; set; }
    public string? ResolutionNotes { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public ICollection<WarrantyClaimItem> Items { get; set; } =
        new List<WarrantyClaimItem>();
    public ICollection<WarrantyClaimEvent> Events { get; set; } =
    new List<WarrantyClaimEvent>();
}
