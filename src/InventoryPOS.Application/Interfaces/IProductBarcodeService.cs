using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.Interfaces;

public interface IProductBarcodeService
{
    Task<IReadOnlyList<ProductBarcodeDto>>
        GetForVariantAsync(
            Guid productVariantId,
            CancellationToken cancellationToken = default);

    Task<ProductBarcodeLookupDto?>
        ResolveAsync(
            string barcode,
            CancellationToken cancellationToken = default);

    Task<ProductBarcodeDto>
        AddAsync(
            Guid productVariantId,
            string barcode,
            BarcodeType type,
            bool isPrimary,
            CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid barcodeId,
        CancellationToken cancellationToken = default);
}