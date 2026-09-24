using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryPOS.Infrastructure.Persistence;

public class InventoryPosDbContextFactory
    : IDesignTimeDbContextFactory<InventoryPosDbContext>
{
    public InventoryPosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<InventoryPosDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;" +
            "Port=5432;" +
            "Database=inventorypos_dev;" +
            "Username=postgres;" +
            "Password=austin316");

        return new InventoryPosDbContext(optionsBuilder.Options);
    }
}