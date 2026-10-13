using InventoryPOS.Application.DTOs.Documents;
using InventoryPOS.Application.Interfaces;

namespace InventoryPOS.Infrastructure.Services;

public sealed class DocumentLookupService
    : IDocumentLookupService
{
    private readonly IDocumentIdentifierService
        _documentIdentifierService;

    public DocumentLookupService(
        IDocumentIdentifierService documentIdentifierService)
    {
        _documentIdentifierService =
            documentIdentifierService;
    }

    public async Task<DocumentLookupResultDto?> ResolveAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var identifier =
            await _documentIdentifierService
                .ResolvePayloadAsync(
                    payload.Trim(),
                    cancellationToken);

        if (identifier is null)
            return null;

        return new DocumentLookupResultDto
        {
            DocumentType =
                identifier.DocumentType,

            DocumentId =
                identifier.DocumentId,

            LookupToken =
                identifier.LookupToken
        };
    }
}