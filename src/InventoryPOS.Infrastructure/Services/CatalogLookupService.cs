using InventoryPOS.Application.DTOs.Lookups;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class CatalogLookupService : ICatalogLookupService
{
    private readonly InventoryPosDbContext _dbContext;

    public CatalogLookupService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryLookupDto>>
        GetCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryLookupDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BrandLookupDto>>
        GetBrandsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Brands
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new BrandLookupDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitLookupDto>>
        GetUnitsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Units
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new UnitLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Symbol = x.Symbol
            })
            .ToListAsync(cancellationToken);
    }
}