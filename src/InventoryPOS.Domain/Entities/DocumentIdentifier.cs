using InventoryPOS.Domain.Common;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Domain.Entities;

public class DocumentIdentifier : BaseEntity
{
    public DocumentType DocumentType { get; set; }

    public Guid DocumentId { get; set; }

    public string LookupToken { get; set; }
        = string.Empty;

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;
}