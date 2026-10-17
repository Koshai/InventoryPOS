using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Tests.Licensing;

public sealed class LicenseDatabaseIntegrationTests
{
    [Fact]
    public async Task ExpiredLicense_BlocksNewCheckout_WithoutCreatingSale()
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var checkoutId = Guid.NewGuid();

        var request = new CreateSaleRequest
        {
            CheckoutRequestId = checkoutId,
            LocationId = Guid.NewGuid(),
            Lines = new List<CreateSaleLineRequest>
            {
                new()
                {
                    ProductVariantId = Guid.NewGuid(),
                    Quantity = 1m,
                    UnitPrice = 100m
                }
            }
        };

        var guard = new RejectingLicenseGuard();

        // The license guard rejects checkout before pricing or financial services.
        var service = new SaleService(db, null!, null!, guard);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateSaleAsync(request));

        Assert.Contains(
            "expired",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(1, guard.CallCount);

        Assert.False(await db.Sales
            .AsNoTracking()
            .AnyAsync(x => x.CheckoutRequestId == checkoutId));
    }

    [Fact]
    public async Task ExpiredLicense_BlocksNewInvoicePayment_WithoutDatabaseWrites()
    {
        await VerifyExpiredLicenseBlocksPaymentAsync(invoicePayment: true);
    }

    [Fact]
    public async Task ExpiredLicense_BlocksNewAccountPayment_WithoutDatabaseWrites()
    {
        await VerifyExpiredLicenseBlocksPaymentAsync(invoicePayment: false);
    }

    private static async Task VerifyExpiredLicenseBlocksPaymentAsync(
        bool invoicePayment)
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var paymentRequestId = Guid.NewGuid();

        var request = new ReceiveCustomerPaymentRequest
        {
            CustomerId = Guid.NewGuid(),
            PaymentRequestId = paymentRequestId,
            Amount = 100m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentDate = DateTime.UtcNow
        };

        var guard = new RejectingLicenseGuard();
        var service = new CustomerAccountService(db, null!, guard);

        var paymentCountBefore = await db.SalePayments.CountAsync();
        var operationCountBefore =
            await db.CustomerPaymentOperations.CountAsync();

        InvalidOperationException exception;

        if (invoicePayment)
        {
            exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ReceiveInvoicePaymentAsync(
                    Guid.NewGuid(), request));
        }
        else
        {
            exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ReceiveAccountPaymentAsync(request));
        }

        Assert.Contains(
            "expired",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(1, guard.CallCount);

        Assert.Equal(paymentCountBefore, await db.SalePayments.CountAsync());
        Assert.Equal(
            operationCountBefore,
            await db.CustomerPaymentOperations.CountAsync());

        Assert.False(await db.CustomerPaymentOperations
            .AsNoTracking()
            .AnyAsync(x => x.PaymentRequestId == paymentRequestId));
    }

    [Fact]
    public async Task ExpiredLicense_CanCheckForPreviouslyCommittedCheckout()
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var guard = new RejectingLicenseGuard();
        var service = new SaleService(db, null!, null!, guard);

        var request = new CreateSaleRequest
        {
            CheckoutRequestId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Lines = new List<CreateSaleLineRequest>
            {
                new()
                {
                    ProductVariantId = Guid.NewGuid(),
                    Quantity = 1m,
                    UnitPrice = 100m
                }
            }
        };

        var result = await service.FindCommittedCheckoutAsync(request);

        Assert.Null(result);
        Assert.Equal(0, guard.CallCount);

        Assert.False(await db.Sales
            .AsNoTracking()
            .AnyAsync(x =>
                x.CheckoutRequestId == request.CheckoutRequestId));
    }

    [Fact]
    public async Task ExpiredLicense_RecoversCommittedCheckout_WithoutDuplicateSaleOrStockDeduction()
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var fixture = await LicenseRecoveryDataFixture.CreateAsync(
            db, includeCustomer: false);

        try
        {
            var checkoutId = Guid.NewGuid();

            var request = new CreateSaleRequest
            {
                CheckoutRequestId = checkoutId,
                LocationId = fixture.LocationId,
                Lines = new List<CreateSaleLineRequest>
                {
                    new()
                    {
                        ProductVariantId = fixture.VariantId,
                        Quantity = 2m,
                        UnitPrice = 100m
                    }
                },
                Payments = new List<CreateSalePaymentRequest>
                {
                    new()
                    {
                        PaymentMethod = PaymentMethod.Cash,
                        Amount = 200m
                    }
                }
            };

            var acceptingGuard = new AcceptingLicenseGuard();

            var firstService = new SaleService(
                db, null!, null!, acceptingGuard);

            // Phase 1: Commit checkout under a usable license.
            var original = await firstService.CreateSaleAsync(request);

            Assert.NotEqual(Guid.Empty, original.SaleId);
            Assert.Equal(1, acceptingGuard.CallCount);

            var stockAfterFirstSale = await db.StockBalances
                .AsNoTracking()
                .Where(x => x.Id == fixture.StockBalanceId)
                .Select(x => x.QuantityOnHand)
                .SingleAsync();

            Assert.Equal(8m, stockAfterFirstSale);

            // Phase 2: Recover checkout after simulated expiry.
            await using var recoveryDb =
                await PostgresLicenseTestFixture.OpenAsync();

            var expiredGuard = new RejectingLicenseGuard();

            var recoveryService = new SaleService(
                recoveryDb, null!, null!, expiredGuard);

            var recovered =
                await recoveryService.CreateSaleAsync(request);

            Assert.Equal(original.SaleId, recovered.SaleId);
            Assert.Equal(original.InvoiceNumber, recovered.InvoiceNumber);
            Assert.Equal(0, expiredGuard.CallCount);

            Assert.Equal(
                1,
                await recoveryDb.Sales.CountAsync(
                    x => x.CheckoutRequestId == checkoutId));

            var stockAfterRecovery = await recoveryDb.StockBalances
                .AsNoTracking()
                .Where(x => x.Id == fixture.StockBalanceId)
                .Select(x => x.QuantityOnHand)
                .SingleAsync();

            Assert.Equal(8m, stockAfterRecovery);

            Assert.Equal(
                1,
                await recoveryDb.SaleLines.CountAsync(
                    x => x.SaleId == original.SaleId));

            Assert.Equal(
                1,
                await recoveryDb.SalePayments.CountAsync(
                    x => x.SaleId == original.SaleId));
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    public async Task ExpiredLicense_RecoversCommittedInvoicePayment_WithoutDuplicatePayment()
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var fixture = await LicenseRecoveryDataFixture.CreateAsync(
            db, includeCustomer: true);

        try
        {
            var sale = await fixture.CreateCreditSaleAsync(
                db, new AcceptingLicenseGuard());

            Assert.Equal(200m, sale.DueAmount);

            var paymentRequest = new ReceiveCustomerPaymentRequest
            {
                CustomerId = fixture.CustomerId!.Value,
                PaymentRequestId = Guid.NewGuid(),
                Amount = 75m,
                PaymentMethod = PaymentMethod.Cash,
                PaymentDate = DateTime.UtcNow
            };

            var acceptingGuard = new AcceptingLicenseGuard();

            var paymentService = new CustomerAccountService(
                db,
                new SaleFinancialService(db),
                acceptingGuard);

            // Phase 1: Commit an invoice payment.
            var original = await paymentService.ReceiveInvoicePaymentAsync(
                sale.SaleId, paymentRequest);

            Assert.Equal(1, acceptingGuard.CallCount);
            Assert.Equal(75m, original.AmountReceived);
            Assert.Equal(75m, original.AmountApplied);

            // Phase 2: Recover the payment after simulated expiry.
            await using var recoveryDb =
                await PostgresLicenseTestFixture.OpenAsync();

            var expiredGuard = new RejectingLicenseGuard();

            var recoveryService = new CustomerAccountService(
                recoveryDb,
                new SaleFinancialService(recoveryDb),
                expiredGuard);

            var recovered = await recoveryService.ReceiveInvoicePaymentAsync(
                sale.SaleId, paymentRequest);

            Assert.Equal(original.AmountReceived, recovered.AmountReceived);
            Assert.Equal(original.AmountApplied, recovered.AmountApplied);
            Assert.Equal(
                original.RemainingOutstanding,
                recovered.RemainingOutstanding);

            Assert.Equal(0, expiredGuard.CallCount);

            Assert.Equal(
                1,
                await recoveryDb.CustomerPaymentOperations.CountAsync(
                    x => x.PaymentRequestId ==
                         paymentRequest.PaymentRequestId));

            Assert.Equal(
                1,
                await recoveryDb.SalePayments.CountAsync(
                    x => x.SaleId == sale.SaleId));

            var financialState = await new SaleFinancialService(recoveryDb)
                .GetStateAsync(sale.SaleId);

            Assert.Equal(125m, financialState.DueAmount);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    public async Task ExpiredLicense_RecoversCommittedAccountPayment_WithoutDuplicatePayment()
    {
        await using var db =
            await PostgresLicenseTestFixture.OpenAsync();

        var fixture = await LicenseRecoveryDataFixture.CreateAsync(
            db, includeCustomer: true);

        try
        {
            var sale = await fixture.CreateCreditSaleAsync(
                db, new AcceptingLicenseGuard());

            Assert.Equal(200m, sale.DueAmount);

            var paymentRequest = new ReceiveCustomerPaymentRequest
            {
                CustomerId = fixture.CustomerId!.Value,
                PaymentRequestId = Guid.NewGuid(),
                Amount = 75m,
                PaymentMethod = PaymentMethod.Cash,
                PaymentDate = DateTime.UtcNow
            };

            var acceptingGuard = new AcceptingLicenseGuard();

            var paymentService = new CustomerAccountService(
                db,
                new SaleFinancialService(db),
                acceptingGuard);

            // Phase 1: Commit a FIFO account payment.
            var original = await paymentService.ReceiveAccountPaymentAsync(
                paymentRequest);

            Assert.Equal(1, acceptingGuard.CallCount);
            Assert.Equal(75m, original.AmountReceived);
            Assert.Equal(75m, original.AmountApplied);

            // Phase 2: Recover the payment after simulated expiry.
            await using var recoveryDb =
                await PostgresLicenseTestFixture.OpenAsync();

            var expiredGuard = new RejectingLicenseGuard();

            var recoveryService = new CustomerAccountService(
                recoveryDb,
                new SaleFinancialService(recoveryDb),
                expiredGuard);

            var recovered = await recoveryService.ReceiveAccountPaymentAsync(
                paymentRequest);

            Assert.Equal(original.AmountReceived, recovered.AmountReceived);
            Assert.Equal(original.AmountApplied, recovered.AmountApplied);
            Assert.Equal(
                original.RemainingOutstanding,
                recovered.RemainingOutstanding);

            Assert.Equal(0, expiredGuard.CallCount);

            Assert.Equal(
                1,
                await recoveryDb.CustomerPaymentOperations.CountAsync(
                    x => x.PaymentRequestId ==
                         paymentRequest.PaymentRequestId));

            Assert.Equal(
                1,
                await recoveryDb.SalePayments.CountAsync(
                    x => x.SaleId == sale.SaleId));

            var financialState = await new SaleFinancialService(recoveryDb)
                .GetStateAsync(sale.SaleId);

            Assert.Equal(125m, financialState.DueAmount);
        }
        finally
        {
            await fixture.CleanupAsync();
        }
    }

    private sealed class RejectingLicenseGuard : ILicenseAccessGuard
    {
        public int CallCount { get; private set; }

        public void EnsureCanWrite()
        {
            CallCount++;

            throw new LicenseAccessDeniedException(
                "License expired: write operations are disabled.");
        }
    }

    private sealed class AcceptingLicenseGuard : ILicenseAccessGuard
    {
        public int CallCount { get; private set; }

        public void EnsureCanWrite()
        {
            CallCount++;
        }
    }
}