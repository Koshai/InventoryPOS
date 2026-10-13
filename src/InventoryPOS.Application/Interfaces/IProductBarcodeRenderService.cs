using InventoryPOS.Application.DTOs.Products;

namespace InventoryPOS.Application.Interfaces;

public interface IProductBarcodeRenderService
{
    Task<IReadOnlyDictionary<Guid, ProductBarcodeRenderDto>>
        GeneratePrimaryBarcodesAsync(
            IEnumerable<Guid> productVariantIds,
            CancellationToken cancellationToken = default);

    Task<ProductBarcodeRenderDto?>
        GenerateBarcodeAsync(
            Guid productBarcodeId,
            CancellationToken cancellationToken = default);
}