
using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Application.Validation;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Infrastructure.Services;

public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly InventoryPosDbContext _dbContext;
    private readonly ISaleFinancialService _financialService;
    private readonly ILicenseAccessGuard _licenseAccessGuard;

    public CustomerAccountService(
    InventoryPosDbContext dbContext,
    ISaleFinancialService financialService,
    ILicenseAccessGuard licenseAccessGuard)
    {
        _dbContext = dbContext;
        _financialService = financialService;
        _licenseAccessGuard = licenseAccessGuard;
    }

    // ============================================================
    // GET CUSTOMER ACCOUNT
    // ============================================================

    public async Task<CustomerAccountDto?> GetAccountAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Id == customerId)
            .Select(x => new
            {
                x.Id,
                x.CustomerCode,
                x.Name,
                x.Phone,
                x.CreditLimit
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
            return null;

        var sales = await _dbContext.Sales
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status != SaleStatus.Cancelled &&
                x.Status != SaleStatus.Refunded)
            .OrderBy(x => x.SaleDate)
            .ThenBy(x => x.InvoiceNumber)
            .Select(x => new
            {
                x.Id,
                x.InvoiceNumber,
                x.SaleDate
            })
            .ToListAsync(cancellationToken);

        var financialStates = await _financialService
            .GetStatesAsync(
                sales.Select(x => x.Id),
                cancellationToken);

        var outstandingSales =
            new List<CustomerOutstandingSaleDto>();

        foreach (var sale in sales)
        {
            var state = financialStates[sale.Id];

            if (state.DueAmount <= 0m)
                continue;

            outstandingSales.Add(
                new CustomerOutstandingSaleDto
                {
                    SaleId = sale.Id,
                    InvoiceNumber = sale.InvoiceNumber,
                    SaleDate = sale.SaleDate,
                    TotalAmount = state.EffectiveTotal,
                    PaidAmount = state.NetPaidAmount,
                    DueAmount = state.DueAmount,
                    Status = state.NetPaidAmount > 0m
                        ? SaleStatus.PartiallyPaid
                        : SaleStatus.Credit
                });
        }

        var paymentHistory = await _dbContext.SalePayments
            .AsNoTracking()
            .Where(x =>
                x.Sale != null &&
                x.Sale.CustomerId == customerId)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new CustomerPaymentHistoryDto
            {
                SalePaymentId = x.Id,
                SaleId = x.SaleId,
                InvoiceNumber = x.Sale!.InvoiceNumber,
                PaymentDate = x.PaymentDate,
                PaymentMethod = x.PaymentMethod,
                Amount = x.Amount,
                ReferenceNumber = x.ReferenceNumber,
                Notes = x.Notes
            })
            .ToListAsync(cancellationToken);

        return new CustomerAccountDto
        {
            CustomerId = customer.Id,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.Name,
            Phone = customer.Phone,
            CreditLimit = customer.CreditLimit,
            OutstandingBalance =
                outstandingSales.Sum(x => x.DueAmount),
            OutstandingSales = outstandingSales,
            PaymentHistory = paymentHistory
        };
    }

    // ============================================================
    // RECEIVE PAYMENT FOR ONE INVOICE
    // ============================================================

    public async Task<CustomerPaymentResultDto>
        ReceiveInvoicePaymentAsync(
            Guid saleId,
            ReceiveCustomerPaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidatePaymentRequest(request);

        if (saleId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "An invoice is required.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await LockPaymentCustomerAsync(
                request.CustomerId,
                cancellationToken);

            var fingerprint = GetPaymentFingerprint(
                saleId,
                request);

            var previous = await FindPreviousPaymentAsync(
                request,
                fingerprint,
                cancellationToken);

            if (previous is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return previous;
            }

            // Allow recovery of a previously committed payment.
            // Require a usable license for a genuinely new payment.
            _licenseAccessGuard.EnsureCanWrite();

            var sale = await _dbContext.Sales
                .FirstOrDefaultAsync(
                    x => x.Id == saleId,
                    cancellationToken);

            if (sale is null)
            {
                throw new InvalidOperationException(
                    "Sale was not found.");
            }

            if (sale.CustomerId != request.CustomerId)
            {
                throw new InvalidOperationException(
                    "The selected invoice does not belong to this customer.");
            }

            EnsureSaleCanReceivePayment(sale);

            var state = await _financialService
                .GetStateAsync(
                    sale.Id,
                    cancellationToken);

            if (state.DueAmount <= 0m)
            {
                throw new InvalidOperationException(
                    "This invoice is already fully paid.");
            }

            if (request.Amount > state.DueAmount)
            {
                throw new InvalidOperationException(
                    $"Payment cannot exceed the invoice due amount of {state.DueAmount:N2}.");
            }

            var payment = CreatePayment(
                sale.Id,
                request,
                request.Amount);

            _dbContext.SalePayments.Add(payment);

            var newNetPaid =
                state.NetPaidAmount + request.Amount;

            ApplyPaymentState(
                sale,
                state.EffectiveTotal,
                newNetPaid);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            var remainingOutstanding =
                await CalculateOutstandingBalanceAsync(
                    request.CustomerId,
                    cancellationToken);

            var result = new CustomerPaymentResultDto
            {
                AmountReceived = request.Amount,
                AmountApplied = request.Amount,
                RemainingOutstanding = remainingOutstanding,
                Allocations = new List<CustomerPaymentAllocationDto>
                {
                    new()
                    {
                        SaleId = sale.Id,
                        InvoiceNumber = sale.InvoiceNumber,
                        AmountApplied = request.Amount,
                        RemainingDue = sale.DueAmount
                    }
                }
            };

            RecordPaymentOperation(
                saleId,
                request,
                fingerprint,
                result);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    // ============================================================
    // RECEIVE GENERAL ACCOUNT PAYMENT - FIFO
    // ============================================================

    public async Task<CustomerPaymentResultDto>
        ReceiveAccountPaymentAsync(
            ReceiveCustomerPaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidatePaymentRequest(request);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await LockPaymentCustomerAsync(
                request.CustomerId,
                cancellationToken);

            var fingerprint = GetPaymentFingerprint(
                null,
                request);

            var previous = await FindPreviousPaymentAsync(
                request,
                fingerprint,
                cancellationToken);

            if (previous is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return previous;
            }

            // Existing payment recovery is permitted.
            // New customer account payments require a valid license.
            _licenseAccessGuard.EnsureCanWrite();

            var customerExists = await _dbContext.Customers
                .AnyAsync(
                    x => x.Id == request.CustomerId &&
                         x.IsActive,
                    cancellationToken);

            if (!customerExists)
            {
                throw new InvalidOperationException(
                    "Customer was not found or is inactive.");
            }

            var sales = await _dbContext.Sales
                .Where(x =>
                    x.CustomerId == request.CustomerId &&
                    x.Status != SaleStatus.Cancelled &&
                    x.Status != SaleStatus.Refunded)
                .OrderBy(x => x.SaleDate)
                .ThenBy(x => x.InvoiceNumber)
                .ToListAsync(cancellationToken);

            if (sales.Count == 0)
            {
                throw new InvalidOperationException(
                    "This customer has no outstanding invoices.");
            }

            var states = await _financialService
                .GetStatesAsync(
                    sales.Select(x => x.Id),
                    cancellationToken);

            var outstandingSales =
                new List<OutstandingSaleState>();

            foreach (var sale in sales)
            {
                var state = states[sale.Id];

                if (state.DueAmount <= 0m)
                    continue;

                outstandingSales.Add(
                    new OutstandingSaleState
                    {
                        Sale = sale,
                        EffectiveTotal = state.EffectiveTotal,
                        NetPaidAmount = state.NetPaidAmount,
                        DueAmount = state.DueAmount
                    });
            }

            if (outstandingSales.Count == 0)
            {
                throw new InvalidOperationException(
                    "This customer has no outstanding balance.");
            }

            var totalOutstanding =
                outstandingSales.Sum(x => x.DueAmount);

            if (request.Amount > totalOutstanding)
            {
                throw new InvalidOperationException(
                    $"Payment cannot exceed the customer's outstanding balance of {totalOutstanding:N2}.");
            }

            var remaining = request.Amount;

            var result = new CustomerPaymentResultDto
            {
                AmountReceived = request.Amount
            };

            foreach (var item in outstandingSales)
            {
                if (remaining <= 0m)
                    break;

                var amountToApply =
                    Math.Min(remaining, item.DueAmount);

                var payment = CreatePayment(
                    item.Sale.Id,
                    request,
                    amountToApply);

                _dbContext.SalePayments.Add(payment);

                var newNetPaid =
                    item.NetPaidAmount + amountToApply;

                ApplyPaymentState(
                    item.Sale,
                    item.EffectiveTotal,
                    newNetPaid);

                remaining -= amountToApply;

                result.Allocations.Add(
                    new CustomerPaymentAllocationDto
                    {
                        SaleId = item.Sale.Id,
                        InvoiceNumber = item.Sale.InvoiceNumber,
                        AmountApplied = amountToApply,
                        RemainingDue = item.Sale.DueAmount
                    });
            }

            result.AmountApplied =
                request.Amount - remaining;

            if (result.AmountApplied != request.Amount)
            {
                throw new InvalidOperationException(
                    "The full customer payment could not be allocated.");
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            result.RemainingOutstanding =
                await CalculateOutstandingBalanceAsync(
                    request.CustomerId,
                    cancellationToken);

            RecordPaymentOperation(
                null,
                request,
                fingerprint,
                result);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    // ============================================================
    // CUSTOMER PAYMENT SERIALIZATION
    // ============================================================

    private async Task LockPaymentCustomerAsync(
    Guid customerId,
    CancellationToken cancellationToken)
    {
        var lockKey =
            "InventoryPOS:CustomerPayment:" +
            customerId.ToString("D");

        await _dbContext.Database
            .SqlQueryRaw<int>(
                """
            SELECT 1 AS "Value"
            FROM (
                SELECT pg_advisory_xact_lock(
                    hashtextextended({0}, 0)
                )
            ) AS payment_lock
            """,
                lockKey)
            .SingleAsync(cancellationToken);
    }

    // ============================================================
    // REQUEST FINGERPRINT
    // ============================================================

    private static string GetPaymentFingerprint(
        Guid? targetSaleId,
        ReceiveCustomerPaymentRequest request)
    {
        var fields = new[]
        {
            request.CustomerId.ToString("D"),
            targetSaleId?.ToString("D") ?? "",
            request.Amount.ToString(
                "0.00", CultureInfo.InvariantCulture),
            ((int)request.PaymentMethod).ToString(
                CultureInfo.InvariantCulture),
            request.PaymentDate.ToUniversalTime()
                .Ticks.ToString(CultureInfo.InvariantCulture),
            Clean(request.ReferenceNumber) ?? "",
            Clean(request.Notes) ?? ""
        };

        var payload = JsonSerializer.Serialize(fields);

        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(payload)));
    }

    // ============================================================
    // EXISTING PAYMENT REPLAY
    // ============================================================

    private async Task<CustomerPaymentResultDto?>
        FindPreviousPaymentAsync(
            ReceiveCustomerPaymentRequest request,
            string fingerprint,
            CancellationToken cancellationToken)
    {
        var operation =
            await _dbContext.CustomerPaymentOperations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.PaymentRequestId ==
                         request.PaymentRequestId,
                    cancellationToken);

        if (operation is null)
            return null;

        if (operation.RequestFingerprint != fingerprint ||
            operation.CustomerId != request.CustomerId)
        {
            throw new InvalidOperationException(
                "Payment request ID was reused with different payment details.");
        }

        return JsonSerializer
            .Deserialize<CustomerPaymentResultDto>(
                operation.ResultJson)
            ?? throw new InvalidOperationException(
                "The stored payment result is invalid.");
    }

    // ============================================================
    // PERSIST PAYMENT OPERATION
    // ============================================================

    private void RecordPaymentOperation(
        Guid? targetSaleId,
        ReceiveCustomerPaymentRequest request,
        string fingerprint,
        CustomerPaymentResultDto result)
    {
        _dbContext.CustomerPaymentOperations.Add(
            new CustomerPaymentOperation
            {
                PaymentRequestId = request.PaymentRequestId,
                CustomerId = request.CustomerId,
                TargetSaleId = targetSaleId,
                Amount = request.Amount,
                RequestFingerprint = fingerprint,
                ResultJson = JsonSerializer.Serialize(result)
            });
    }

    // ============================================================
    // CUSTOMER OUTSTANDING
    // ============================================================

    private async Task<decimal>
        CalculateOutstandingBalanceAsync(
            Guid customerId,
            CancellationToken cancellationToken)
    {
        var saleIds = await _dbContext.Sales
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status != SaleStatus.Cancelled &&
                x.Status != SaleStatus.Refunded)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (saleIds.Count == 0)
            return 0m;

        var states = await _financialService
            .GetStatesAsync(
                saleIds,
                cancellationToken);

        return states.Values.Sum(x => x.DueAmount);
    }

    // ============================================================
    // PAYMENT CREATION
    // ============================================================

    private static SalePayment CreatePayment(
        Guid saleId,
        ReceiveCustomerPaymentRequest request,
        decimal amount)
    {
        return new SalePayment
        {
            SaleId = saleId,
            PaymentRequestId = request.PaymentRequestId,
            PaymentMethod = request.PaymentMethod,
            Amount = amount,
            PaymentDate = request.PaymentDate,
            ReferenceNumber = Clean(request.ReferenceNumber),
            Notes = Clean(request.Notes)
        };
    }

    // ============================================================
    // PAYMENT STATE
    // ============================================================

    private static void ApplyPaymentState(
        Sale sale,
        decimal effectiveTotal,
        decimal netPaidAmount)
    {
        if (effectiveTotal < 0m)
        {
            throw new InvalidOperationException(
                $"Invoice {sale.InvoiceNumber} has an invalid effective total.");
        }

        if (netPaidAmount < 0m)
        {
            throw new InvalidOperationException(
                $"Invoice {sale.InvoiceNumber} has an invalid net payment total.");
        }

        sale.PaidAmount = netPaidAmount;

        sale.DueAmount =
            Math.Max(0m, effectiveTotal - netPaidAmount);

        if (sale.DueAmount <= 0m)
        {
            sale.Status = SaleStatus.Completed;
            return;
        }

        sale.Status = netPaidAmount > 0m
            ? SaleStatus.PartiallyPaid
            : SaleStatus.Credit;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidatePaymentRequest(
        ReceiveCustomerPaymentRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.CustomerId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Customer is required.");
        }

        if (request.PaymentRequestId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Payment request ID is required.");
        }

        if (request.PaymentDate == default)
        {
            throw new InvalidOperationException(
                "Payment date is required.");
        }

        PaymentValidator.Validate(
            request.PaymentMethod,
            request.Amount,
            request.ReferenceNumber,
            request.Notes);
    }

    private static void EnsureSaleCanReceivePayment(
        Sale sale)
    {
        if (sale.Status == SaleStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Payments cannot be applied to a cancelled sale.");
        }

        if (sale.Status == SaleStatus.Refunded)
        {
            throw new InvalidOperationException(
                "Payments cannot be applied to a refunded sale.");
        }

        if (sale.CustomerId is null)
        {
            throw new InvalidOperationException(
                "Payments cannot be applied to a walk-in credit account.");
        }
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed class OutstandingSaleState
    {
        public required Sale Sale { get; set; }

        public decimal EffectiveTotal { get; set; }

        public decimal NetPaidAmount { get; set; }

        public decimal DueAmount { get; set; }
    }
}
