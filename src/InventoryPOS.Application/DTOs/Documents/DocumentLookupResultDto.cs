using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Documents;

public sealed class DocumentLookupResultDto
{
    public DocumentType DocumentType { get; init; }

    public Guid DocumentId { get; init; }

    public string LookupToken { get; init; } =
        string.Empty;
}