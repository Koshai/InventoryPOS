using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class WarrantyClaimEvent : BaseEntity
{
    public Guid WarrantyClaimId { get; set; }

    public WarrantyClaimEventType EventType { get; set; }

    public WarrantyClaimStatus? FromStatus { get; set; }

    public WarrantyClaimStatus? ToStatus { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Notes { get; set; }

    // Reserved for the authentication/authorization module.
    public Guid? UserId { get; set; }

    public WarrantyClaim WarrantyClaim { get; set; } = null!;
}