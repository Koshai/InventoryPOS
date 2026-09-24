using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly InventoryPosDbContext _dbContext;

    public CustomerAccountService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var outstandingSales = await _dbContext.Sales
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.DueAmount > 0 &&
                x.Status != SaleStatus.Cancelled &&
                x.Status != SaleStatus.Refunded)
            .OrderBy(x => x.SaleDate)
            .ThenBy(x => x.InvoiceNumber)
            .Select(x => new CustomerOutstandingSaleDto
            {
                SaleId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                SaleDate = x.SaleDate,
                TotalAmount = x.TotalAmount,
                PaidAmount = x.PaidAmount,
                DueAmount = x.DueAmount,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);

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

        var outstandingBalance =
            outstandingSales.Sum(x => x.DueAmount);

        return new CustomerAccountDto
        {
            CustomerId = customer.Id,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.Name,
            Phone = customer.Phone,
            CreditLimit = customer.CreditLimit,
            OutstandingBalance = outstandingBalance,
            OutstandingSales = outstandingSales,
            PaymentHistory = paymentHistory
        };
    }

    // ============================================================
    // RECEIVE PAYMENT FOR ONE SPECIFIC INVOICE
    // ============================================================

    public async Task<CustomerPaymentResultDto>
        ReceiveInvoicePaymentAsync(
            Guid saleId,
            ReceiveCustomerPaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidatePaymentRequest(request);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
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

            // Payment rows are authoritative.
            var existingPaidAmount =
                await GetPaidAmountAsync(
                    sale.Id,
                    cancellationToken);

            ValidateExistingPaymentState(
                sale,
                existingPaidAmount);

            var currentDue =
                Math.Max(
                    0m,
                    sale.TotalAmount - existingPaidAmount);

            if (currentDue <= 0)
            {
                throw new InvalidOperationException(
                    "This invoice is already fully paid.");
            }

            if (request.Amount > currentDue)
            {
                throw new InvalidOperationException(
                    $"Payment cannot exceed the invoice due amount of {currentDue:N2}.");
            }

            var payment = CreatePayment(
                sale.Id,
                request,
                request.Amount);

            _dbContext.SalePayments.Add(payment);

            var newPaidAmount =
                existingPaidAmount + request.Amount;

            ApplyPaymentState(
                sale,
                newPaidAmount);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var remainingOutstanding =
                await GetOutstandingBalanceAsync(
                    request.CustomerId,
                    cancellationToken);

            return new CustomerPaymentResultDto
            {
                AmountReceived =
                    request.Amount,

                AmountApplied =
                    request.Amount,

                RemainingOutstanding =
                    remainingOutstanding,

                Allocations =
                {
                    new CustomerPaymentAllocationDto
                    {
                        SaleId =
                            sale.Id,

                        InvoiceNumber =
                            sale.InvoiceNumber,

                        AmountApplied =
                            request.Amount,

                        RemainingDue =
                            sale.DueAmount
                    }
                }
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    // ============================================================
    // RECEIVE GENERAL CUSTOMER ACCOUNT PAYMENT
    //
    // FIFO:
    // oldest outstanding invoice receives payment first.
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
            var customerExists =
                await _dbContext.Customers
                    .AnyAsync(
                        x =>
                            x.Id == request.CustomerId &&
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
                    "This customer has no sales eligible for payment.");
            }

            /*
             * We intentionally calculate the outstanding amounts from
             * SalePayment records instead of trusting the cached
             * Sale.PaidAmount / Sale.DueAmount values.
             */

            var saleIds =
                sales.Select(x => x.Id)
                    .ToList();

            var paymentTotals =
                await _dbContext.SalePayments
                    .AsNoTracking()
                    .Where(x =>
                        saleIds.Contains(x.SaleId))
                    .GroupBy(x => x.SaleId)
                    .Select(x => new
                    {
                        SaleId = x.Key,
                        PaidAmount =
                            x.Sum(y => y.Amount)
                    })
                    .ToDictionaryAsync(
                        x => x.SaleId,
                        x => x.PaidAmount,
                        cancellationToken);

            var outstandingSales =
                new List<(Sale Sale, decimal Paid, decimal Due)>();

            foreach (var sale in sales)
            {
                var paid =
                    paymentTotals.TryGetValue(
                        sale.Id,
                        out var paymentTotal)
                            ? paymentTotal
                            : 0m;

                ValidateExistingPaymentState(
                    sale,
                    paid);

                var due =
                    Math.Max(
                        0m,
                        sale.TotalAmount - paid);

                if (due > 0)
                {
                    outstandingSales.Add(
                        (sale, paid, due));
                }
            }

            if (outstandingSales.Count == 0)
            {
                throw new InvalidOperationException(
                    "This customer has no outstanding invoices.");
            }

            var totalOutstanding =
                outstandingSales.Sum(x => x.Due);

            if (request.Amount > totalOutstanding)
            {
                throw new InvalidOperationException(
                    $"Payment cannot exceed the customer's outstanding balance of {totalOutstanding:N2}.");
            }

            var remaining =
                request.Amount;

            var result =
                new CustomerPaymentResultDto
                {
                    AmountReceived =
                        request.Amount
                };

            foreach (var item in outstandingSales)
            {
                if (remaining <= 0)
                    break;

                var amountToApply =
                    Math.Min(
                        remaining,
                        item.Due);

                if (amountToApply <= 0)
                    continue;

                var payment =
                    CreatePayment(
                        item.Sale.Id,
                        request,
                        amountToApply);

                _dbContext.SalePayments.Add(
                    payment);

                var newPaidAmount =
                    item.Paid + amountToApply;

                ApplyPaymentState(
                    item.Sale,
                    newPaidAmount);

                remaining -=
                    amountToApply;

                result.Allocations.Add(
                    new CustomerPaymentAllocationDto
                    {
                        SaleId =
                            item.Sale.Id,

                        InvoiceNumber =
                            item.Sale.InvoiceNumber,

                        AmountApplied =
                            amountToApply,

                        RemainingDue =
                            item.Sale.DueAmount
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

            await transaction.CommitAsync(
                cancellationToken);

            result.RemainingOutstanding =
                await GetOutstandingBalanceAsync(
                    request.CustomerId,
                    cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    // ============================================================
    // PAYMENT TOTAL
    // ============================================================

    private async Task<decimal> GetPaidAmountAsync(
        Guid saleId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SalePayments
            .AsNoTracking()
            .Where(x => x.SaleId == saleId)
            .SumAsync(
                x => (decimal?)x.Amount,
                cancellationToken)
            ?? 0m;
    }

    // ============================================================
    // CREATE PAYMENT ENTITY
    // ============================================================

    private static SalePayment CreatePayment(
    Guid saleId,
    ReceiveCustomerPaymentRequest request,
    decimal amount)
    {
        return new SalePayment
        {
            SaleId =
                saleId,

            PaymentMethod =
                request.PaymentMethod,

            Amount =
                amount,

            PaymentDate =
                request.PaymentDate,

            ReferenceNumber =
                Clean(request.ReferenceNumber),

            Notes =
                Clean(request.Notes)
        };
    }

    // ============================================================
    // PAYMENT REQUEST VALIDATION
    // ============================================================

    private static void ValidatePaymentRequest(
        ReceiveCustomerPaymentRequest request)
    {
        if (request.CustomerId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Customer is required.");
        }

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");
        }

        if (request.PaymentDate == default)
        {
            throw new InvalidOperationException(
                "Payment date is required.");
        }

        if (RequiresReference(
                request.PaymentMethod) &&
            string.IsNullOrWhiteSpace(
                request.ReferenceNumber))
        {
            throw new InvalidOperationException(
                "A payment reference is required for this payment method.");
        }
    }

    private static bool RequiresReference(
        PaymentMethod paymentMethod)
    {
        return paymentMethod !=
               PaymentMethod.Cash;
    }

    // ============================================================
    // SALE VALIDATION
    // ============================================================

    private static void EnsureSaleCanReceivePayment(
        Sale sale)
    {
        if (sale.Status ==
            SaleStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Payments cannot be applied to a cancelled sale.");
        }

        if (sale.Status ==
            SaleStatus.Refunded)
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

    private static void ValidateExistingPaymentState(
        Sale sale,
        decimal paidAmount)
    {
        if (paidAmount < 0)
        {
            throw new InvalidOperationException(
                $"Invoice {sale.InvoiceNumber} has an invalid negative payment total.");
        }

        if (paidAmount > sale.TotalAmount)
        {
            throw new InvalidOperationException(
                $"Payments for invoice {sale.InvoiceNumber} exceed the sale total.");
        }
    }

    // ============================================================
    // APPLY SALE PAYMENT SUMMARY
    // ============================================================

    private static void ApplyPaymentState(
        Sale sale,
        decimal paidAmount)
    {
        ValidateExistingPaymentState(
            sale,
            paidAmount);

        sale.PaidAmount =
            paidAmount;

        sale.DueAmount =
            Math.Max(
                0m,
                sale.TotalAmount - paidAmount);

        if (sale.DueAmount <= 0)
        {
            sale.Status =
                SaleStatus.Completed;

            return;
        }

        if (sale.PaidAmount > 0)
        {
            sale.Status =
                SaleStatus.PartiallyPaid;

            return;
        }

        sale.Status =
            SaleStatus.Credit;
    }

    // ============================================================
    // CUSTOMER OUTSTANDING BALANCE
    // ============================================================

    private async Task<decimal>
        GetOutstandingBalanceAsync(
            Guid customerId,
            CancellationToken cancellationToken)
    {
        return await _dbContext.Sales
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.DueAmount > 0 &&
                x.Status != SaleStatus.Cancelled &&
                x.Status != SaleStatus.Refunded)
            .SumAsync(
                x => x.DueAmount,
                cancellationToken);
    }

    // ============================================================
    // STRING CLEANUP
    // ============================================================

    private static string? Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}