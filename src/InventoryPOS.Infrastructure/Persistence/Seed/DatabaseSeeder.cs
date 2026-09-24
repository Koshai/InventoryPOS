using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Persistence.Seed;

public class DatabaseSeeder
{
    private readonly InventoryPosDbContext _dbContext;

    public DatabaseSeeder(InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        await SeedUnitsAsync();
        await SeedDefaultCategoryAsync();
        await SeedDefaultLocationAsync();

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedUnitsAsync()
    {
        if (await _dbContext.Units.AnyAsync())
            return;

        var units = new[]
        {
            new Unit
            {
                Name = "Piece",
                NameBn = "পিস",
                Symbol = "pcs"
            },
            new Unit
            {
                Name = "Kilogram",
                NameBn = "কিলোগ্রাম",
                Symbol = "kg"
            },
            new Unit
            {
                Name = "Gram",
                NameBn = "গ্রাম",
                Symbol = "g"
            },
            new Unit
            {
                Name = "Liter",
                NameBn = "লিটার",
                Symbol = "L"
            },
            new Unit
            {
                Name = "Milliliter",
                NameBn = "মিলিলিটার",
                Symbol = "ml"
            },
            new Unit
            {
                Name = "Box",
                NameBn = "বক্স",
                Symbol = "box"
            },
            new Unit
            {
                Name = "Pack",
                NameBn = "প্যাক",
                Symbol = "pack"
            }
        };

        await _dbContext.Units.AddRangeAsync(units);
    }

    private async Task SeedDefaultCategoryAsync()
    {
        if (await _dbContext.Categories
                .AnyAsync(x => x.Name == "Uncategorized"))
        {
            return;
        }

        var category = new Category
        {
            Name = "Uncategorized",
            NameBn = "অন্যান্য",
            IsActive = true
        };

        await _dbContext.Categories.AddAsync(category);
    }

    private async Task SeedDefaultLocationAsync()
    {
        if (await _dbContext.Locations.AnyAsync())
            return;

        await _dbContext.Locations.AddAsync(
            new Location
            {
                Name = "Main Store",
                Code = "MAIN",
                LocationType = "Store",
                IsActive = true
            });
    }
}