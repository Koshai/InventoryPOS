using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Infrastructure.Persistence;
using InventoryPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Tests.Licensing;

// Creates unique, test-owned rows in inventorypos_test.
// Cleanup will be added separately after FK dependencies are verified.
internal sealed class LicenseRecoveryDataFixture
{
    public required Guid CategoryId { get; init; }
    public required Guid UnitId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid LocationId { get; init; }
    public required Guid VariantId { get; init; }
    public required Guid StockBalanceId { get; init; }
    public Guid? CustomerId { get; init; }

    public static async Task<LicenseRecoveryDataFixture> CreateAsync(
        InventoryPosDbContext db,
        bool includeCustomer)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var category = new Category { Name = $"LIC-TEST Category {suffix}" };
        var unit = new Unit { Name = $"LIC-TEST Unit {suffix}", Symbol = "pcs" };
        var location = new Location
        {
            Name = $"LIC-TEST Location {suffix}",
            Code = $"LT{suffix}"
        };
        var product = new Product
        {
            Name = $"LIC-TEST Product {suffix}",
            CategoryId = category.Id,
            UnitId = unit.Id
        };
        var variant = new ProductVariant
        {
            ProductId = product.Id,
            SKU = $"LIC-{suffix}",
            CostPrice = 60m,
            SellingPrice = 100m
        };
        var stock = new StockBalance
        {
            ProductVariantId = variant.Id,
            LocationId = location.Id,
            QuantityOnHand = 10m,
            QuantityReserved = 0m
        };

        db.Categories.Add(category);
        db.Units.Add(unit);
        db.Locations.Add(location);
        db.Products.Add(product);
        db.ProductVariants.Add(variant);
        db.StockBalances.Add(stock);

        Customer? customer = null;
        if (includeCustomer)
        {
            customer = new Customer
            {
                CustomerCode = $"LC{suffix}",
                Name = $"LIC-TEST Customer {suffix}",
                CreditLimit = 1000m
            };
            db.Customers.Add(customer);
        }

        await db.SaveChangesAsync();
        return new LicenseRecoveryDataFixture
        {
            CategoryId = category.Id,
            UnitId = unit.Id,
            ProductId = product.Id,
            LocationId = location.Id,
            VariantId = variant.Id,
            StockBalanceId = stock.Id,
            CustomerId = customer?.Id
        };
    }

    public async Task<SaleResultDto> CreateCreditSaleAsync(
        InventoryPosDbContext db,
        ILicenseAccessGuard guard)
    {
        if (CustomerId is null)
            throw new InvalidOperationException("Fixture has no customer.");

        var service = new SaleService(
            db, null!, new SaleFinancialService(db), guard);

        return await service.CreateSaleAsync(new CreateSaleRequest
        {
            CheckoutRequestId = Guid.NewGuid(),
            LocationId = LocationId,
            CustomerId = CustomerId.Value,
            Lines = new List<CreateSaleLineRequest>
            {
                new()
                {
                    ProductVariantId = VariantId,
                    Quantity = 2m,
                    UnitPrice = 100m
                }
            }
        });
    }
    // Removes only this fixture's rows, using a fresh validated test DB context.
    // The transaction ensures partial cleanup is rolled back if any FK rejects deletion.
    public async Task CleanupAsync()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        // These operations are not linked by a database FK to the sale.
        if (CustomerId is Guid customerId)
        {
            await db.CustomerPaymentOperations
                .Where(x => x.CustomerId == customerId)
                .ExecuteDeleteAsync();
        }

        // Stock movements reference both variant and location with Restrict.
        await db.StockMovements
            .Where(x => x.LocationId == LocationId &&
                        x.ProductVariantId == VariantId)
            .ExecuteDeleteAsync();

        // SaleLine and SalePayment cascade from Sale.
        await db.Sales
            .Where(x => x.LocationId == LocationId)
            .ExecuteDeleteAsync();

        await db.StockBalances
            .Where(x => x.Id == StockBalanceId)
            .ExecuteDeleteAsync();

        await db.ProductVariants
            .Where(x => x.Id == VariantId)
            .ExecuteDeleteAsync();

        await db.Products
            .Where(x => x.Id == ProductId)
            .ExecuteDeleteAsync();

        if (CustomerId is Guid id)
        {
            await db.Customers
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync();
        }

        await db.Categories
            .Where(x => x.Id == CategoryId)
            .ExecuteDeleteAsync();

        await db.Units
            .Where(x => x.Id == UnitId)
            .ExecuteDeleteAsync();

        await db.Locations
            .Where(x => x.Id == LocationId)
            .ExecuteDeleteAsync();

        // Shared document-number counters are intentionally retained.
        await transaction.CommitAsync();
    }

}
