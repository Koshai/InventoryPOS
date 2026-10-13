using InventoryPOS.Application.DTOs.Documents;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Infrastructure.Services;

public sealed class DocumentQrService
    : IDocumentQrService
{
    private readonly IDocumentIdentifierService
        _documentIdentifierService;

    private readonly IQrCodeService
        _qrCodeService;

    public DocumentQrService(
        IDocumentIdentifierService documentIdentifierService,
        IQrCodeService qrCodeService)
    {
        _documentIdentifierService =
            documentIdentifierService;

        _qrCodeService =
            qrCodeService;
    }

    public async Task<DocumentQrDto> GenerateAsync(
        DocumentType documentType,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var identifier =
            await _documentIdentifierService
                .GetOrCreateAsync(
                    documentType,
                    documentId,
                    cancellationToken);

        var pngBytes =
            _qrCodeService.GeneratePng(
                identifier.QrPayload,
                pixelsPerModule: 10);

        return new DocumentQrDto
        {
            DocumentType =
                identifier.DocumentType,

            DocumentId =
                identifier.DocumentId,

            LookupToken =
                identifier.LookupToken,

            Payload =
                identifier.QrPayload,

            PngBytes =
                pngBytes
        };
    }
}