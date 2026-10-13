using InventoryPOS.Application.DTOs.Documents;

namespace InventoryPOS.Application.Interfaces;

public interface IDocumentLookupService
{
    Task<DocumentLookupResultDto?> ResolveAsync(
        string payload,
        CancellationToken cancellationToken = default);
}