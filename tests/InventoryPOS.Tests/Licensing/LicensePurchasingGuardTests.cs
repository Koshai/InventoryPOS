
using InventoryPOS.Application.Licensing;
using InventoryPOS.Infrastructure.Services;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicensePurchasingGuardTests
{
    [Fact]
    public async Task ExpiredLicense_BlocksGoodsReceipt()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var service = new GoodsReceiptService(
            db, new RejectingGuard());

        await Assert.ThrowsAsync<LicenseBlockedException>(
            () => service.CreateReceiptAsync(null!));
    }

    [Fact]
    public async Task ExpiredLicense_BlocksPurchaseOrder()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var service = new PurchaseOrderService(
            db, new RejectingGuard());

        await Assert.ThrowsAsync<LicenseBlockedException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task ExpiredLicense_BlocksPurchaseRequisition()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var service = new PurchaseRequisitionService(
            db, new RejectingGuard());

        await Assert.ThrowsAsync<LicenseBlockedException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task ExpiredLicense_BlocksMarkRequisitionSent()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var service = new PurchaseRequisitionService(
            db, new RejectingGuard());

        await Assert.ThrowsAsync<LicenseBlockedException>(
            () => service.MarkAsSentAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExpiredLicense_BlocksCancelRequisition()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var service = new PurchaseRequisitionService(
            db, new RejectingGuard());

        await Assert.ThrowsAsync<LicenseBlockedException>(
            () => service.CancelAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExpiredLicense_BlocksRequisitionConversion()
    {
        await using var db = await PostgresLicenseTestFixture.OpenAsync();

        var service = new PurchaseRequisitionService(
            db, new RejectingGuard());

        await Assert.ThrowsAsync<LicenseBlockedException>(
            () => service.ConvertToPurchaseOrderAsync(
                Guid.Empty, null!));
    }

    private sealed class RejectingGuard : ILicenseAccessGuard
    {
        public void EnsureCanWrite()
        {
            throw new LicenseBlockedException();
        }
    }

    private sealed class LicenseBlockedException : InvalidOperationException
    {
        public LicenseBlockedException()
            : base("License status: Expired.")
        {
        }
    }
}
