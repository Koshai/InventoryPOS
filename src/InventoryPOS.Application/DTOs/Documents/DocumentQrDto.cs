using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Documents;

public sealed class DocumentQrDto
{
    public DocumentType DocumentType { get; init; }

    public Guid DocumentId { get; init; }

    public string LookupToken { get; init; } =
        string.Empty;

    public string Payload { get; init; } =
        string.Empty;

    public byte[] PngBytes { get; init; } =
        Array.Empty<byte>();
}