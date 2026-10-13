using InventoryPOS.Application.DTOs.Documents;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.Interfaces;

public interface IDocumentQrService
{
    Task<DocumentQrDto> GenerateAsync(
        DocumentType documentType,
        Guid documentId,
        CancellationToken cancellationToken = default);
}