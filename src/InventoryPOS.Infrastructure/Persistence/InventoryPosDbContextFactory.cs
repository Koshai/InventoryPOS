using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryPOS.Infrastructure.Persistence;

public class InventoryPosDbContextFactory
    : IDesignTimeDbContextFactory<InventoryPosDbContext>
{
    public InventoryPosDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("INVENTORYPOS_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__InventoryPosDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set INVENTORYPOS_CONNECTION (or ConnectionStrings__InventoryPosDatabase) " +
                "to a PostgreSQL connection string before running EF design-time tools.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<InventoryPosDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new InventoryPosDbContext(optionsBuilder.Options);
    }
}
