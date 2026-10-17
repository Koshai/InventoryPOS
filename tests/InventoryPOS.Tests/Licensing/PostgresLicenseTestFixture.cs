
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventoryPOS.Tests.Licensing;

internal static class PostgresLicenseTestFixture
{
    public static async Task<InventoryPosDbContext> OpenAsync()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "INVENTORYPOS_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "INVENTORYPOS_TEST_CONNECTION is required.");
        }

        var builder =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(
                builder.Database,
                "inventorypos_test",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Integration tests must use inventorypos_test.");
        }

        var hosts = builder.Host.Split(
            ',',
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries);

        if (hosts.Length != 1 ||
            !(hosts[0].Equals(
                "localhost",
                StringComparison.OrdinalIgnoreCase) ||
              hosts[0] == "127.0.0.1"))
        {
            throw new InvalidOperationException(
                "Integration tests require local PostgreSQL.");
        }

        var options =
            new DbContextOptionsBuilder<InventoryPosDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        var db = new InventoryPosDbContext(options);

        if (!await db.Database.CanConnectAsync())
        {
            await db.DisposeAsync();

            throw new InvalidOperationException(
                "Cannot connect to inventorypos_test.");
        }

        return db;
    }
}
