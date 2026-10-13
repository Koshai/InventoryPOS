using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Documents;

public sealed class DocumentIdentifierDto
{
    public DocumentType DocumentType { get; set; }

    public Guid DocumentId { get; set; }

    public string LookupToken { get; set; }
        = string.Empty;

    public string QrPayload { get; set; }
        = string.Empty;
}