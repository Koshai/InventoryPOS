using InventoryPOS.Application.DTOs.Lookups;

namespace InventoryPOS.Application.Interfaces;

public interface ICatalogLookupService
{
    Task<IReadOnlyList<CategoryLookupDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BrandLookupDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitLookupDto>> GetUnitsAsync(
        CancellationToken cancellationToken = default);
}