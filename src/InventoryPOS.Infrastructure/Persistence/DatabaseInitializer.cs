using InventoryPOS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPOS.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<InventoryPosDbContext>();

        await dbContext.Database.MigrateAsync();

        var seeder =
            scope.ServiceProvider
                .GetRequiredService<DatabaseSeeder>();

        await seeder.SeedAsync();
    }
}