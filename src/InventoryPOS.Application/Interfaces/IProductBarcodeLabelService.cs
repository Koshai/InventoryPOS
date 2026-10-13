using InventoryPOS.Application.DTOs.Products;

namespace InventoryPOS.Application.Interfaces;

public interface IProductBarcodeLabelService
{
    Task<byte[]> GenerateLabelPdfAsync(
        ProductBarcodeLabelRequest request,
        CancellationToken cancellationToken = default);
}