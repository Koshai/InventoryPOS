using InventoryPOS.Application.DTOs.Documents;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.Interfaces;

public interface IDocumentIdentifierService
{
    Task<DocumentIdentifierDto> GetOrCreateAsync(
        DocumentType documentType,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<DocumentIdentifierDto?> ResolveTokenAsync(
        string lookupToken,
        CancellationToken cancellationToken = default);

    Task<DocumentIdentifierDto?> ResolvePayloadAsync(
        string qrPayload,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        DocumentType documentType,
        Guid documentId,
        CancellationToken cancellationToken = default);
}