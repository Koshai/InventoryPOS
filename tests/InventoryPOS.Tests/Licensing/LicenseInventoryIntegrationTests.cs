
using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicenseInventoryIntegrationTests
{
    [Fact]
    public async Task ExpiredLicense_BlocksOpeningStock_WithoutDatabaseWrites()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var fixture = await LicenseRecoveryDataFixture.CreateAsync(
            db,
            includeCustomer: false);

        try
        {
            var service = new InventoryService(
                db,
                new RejectingLicenseGuard());

            var initialQuantity = await db.StockBalances
                .AsNoTracking()
                .Where(x => x.Id == fixture.StockBalanceId)
                .Select(x => x.QuantityOnHand)
                .SingleAsync();

            var initialMovements = await db.StockMovements
                .AsNoTracking()
                .CountAsync(x =>
                    x.ProductVariantId == fixture.VariantId &&
                    x.LocationId == fixture.LocationId);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SetOpeningStockAsync(
                    new SetOpeningStockRequest
                    {
                        ProductVariantId = fixture.VariantId,
                        LocationId = fixture.LocationId,
                        Quantity = 5m,
                        UnitCost = 60m
                    }));

            var finalQuantity = await db.StockBalances
                .AsNoTracking()
                .Where(x => x.Id == fixture.StockBalanceId)
                .Select(x => x.QuantityOnHand)
                .SingleAsync();

            var finalMovements = await db.StockMovements
                .AsNoTracking()
                .CountAsync(x =>
                    x.ProductVariantId == fixture.VariantId &&
                    x.LocationId == fixture.LocationId);

            Assert.Equal(initialQuantity, finalQuantity);
            Assert.Equal(initialMovements, finalMovements);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    public async Task ExpiredLicense_BlocksStockAdjustment_WithoutDatabaseWrites()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var fixture = await LicenseRecoveryDataFixture.CreateAsync(
            db,
            includeCustomer: false);

        try
        {
            var service = new StockAdjustmentService(
                db,
                new RejectingLicenseGuard());

            var initialQuantity = await db.StockBalances
                .AsNoTracking()
                .Where(x => x.Id == fixture.StockBalanceId)
                .Select(x => x.QuantityOnHand)
                .SingleAsync();

            var initialMovements = await db.StockMovements
                .AsNoTracking()
                .CountAsync(x =>
                    x.ProductVariantId == fixture.VariantId &&
                    x.LocationId == fixture.LocationId);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateAsync(
                    new CreateStockAdjustmentRequest
                    {
                        ProductVariantId = fixture.VariantId,
                        LocationId = fixture.LocationId,
                        CountedQuantity = 7m,
                        Reason = "License integration test"
                    }));

            var finalQuantity = await db.StockBalances
                .AsNoTracking()
                .Where(x => x.Id == fixture.StockBalanceId)
                .Select(x => x.QuantityOnHand)
                .SingleAsync();

            var finalMovements = await db.StockMovements
                .AsNoTracking()
                .CountAsync(x =>
                    x.ProductVariantId == fixture.VariantId &&
                    x.LocationId == fixture.LocationId);

            var adjustmentCount = await db.StockAdjustments
                .AsNoTracking()
                .CountAsync(x => x.LocationId == fixture.LocationId);

            Assert.Equal(initialQuantity, finalQuantity);
            Assert.Equal(initialMovements, finalMovements);
            Assert.Equal(0, adjustmentCount);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    public async Task ExpiredLicense_InventoryReadsRemainAvailable()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var fixture = await LicenseRecoveryDataFixture.CreateAsync(
            db,
            includeCustomer: false);

        try
        {
            var inventory = new InventoryService(
                db,
                new RejectingLicenseGuard());

            var adjustment = new StockAdjustmentService(
                db,
                new RejectingLicenseGuard());

            var quantity = await adjustment.GetCurrentQuantityAsync(
                fixture.VariantId,
                fixture.LocationId);

            var items = await inventory.GetInventoryAsync();

            Assert.Equal(10m, quantity);

            Assert.Contains(
                items,
                x => x.ProductVariantId == fixture.VariantId &&
                     x.LocationId == fixture.LocationId);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    private sealed class RejectingLicenseGuard : ILicenseAccessGuard
    {
        public void EnsureCanWrite()
        {
            throw new InvalidOperationException(
                "InventoryPOS cannot perform this operation. " +
                "License status: Expired.");
        }
    }
}
