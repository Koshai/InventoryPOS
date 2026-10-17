
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicenseCleanupRelationshipTests
{
    private readonly ITestOutputHelper _output;

    public LicenseCleanupRelationshipTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DisplayFixtureForeignKeyDependencies()
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var fixtureEntities = new HashSet<string>
        {
            "Category",
            "Unit",
            "Location",
            "Customer",
            "Product",
            "ProductVariant",
            "StockBalance",
            "Sale",
            "SaleLine",
            "SalePayment",
            "StockMovement",
            "CustomerPaymentOperation"
        };

        var relationships = db.Model.GetEntityTypes()
            .SelectMany(entity =>
                entity.GetForeignKeys().Select(fk => new
                {
                    Dependent = entity.ClrType.Name,
                    Principal = fk.PrincipalEntityType.ClrType.Name,
                    Properties = string.Join(
                        ", ",
                        fk.Properties.Select(p => p.Name)),
                    DeleteBehavior = fk.DeleteBehavior.ToString()
                }))
            .Where(x =>
                fixtureEntities.Contains(x.Dependent) ||
                fixtureEntities.Contains(x.Principal))
            .OrderBy(x => x.Principal)
            .ThenBy(x => x.Dependent)
            .ToList();

        foreach (var relationship in relationships)
        {
            _output.WriteLine(
                $"{relationship.Dependent} -> " +
                $"{relationship.Principal} | " +
                $"{relationship.Properties} | " +
                $"{relationship.DeleteBehavior}");
        }

        Assert.NotEmpty(relationships);
    }
}
