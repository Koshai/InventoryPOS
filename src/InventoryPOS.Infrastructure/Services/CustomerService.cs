using InventoryPOS.Application.Licensing;
using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly InventoryPosDbContext _dbContext;

    private readonly ILicenseAccessGuard _licenseAccessGuard;


    public CustomerService(
        InventoryPosDbContext dbContext,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _licenseAccessGuard = licenseAccessGuard ?? throw new ArgumentNullException(nameof(licenseAccessGuard));
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerListItemDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CustomerListItemDto
            {
                Id = x.Id,
                CustomerCode = x.CustomerCode,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                CreditLimit = x.CreditLimit,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDetailsDto?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Id == customerId)
            .Select(x => new CustomerDetailsDto
            {
                Id = x.Id,
                CustomerCode = x.CustomerCode,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                CreditLimit = x.CreditLimit,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        Validate(
            request.CustomerCode,
            request.Name,
            request.CreditLimit);

        var customerCode =
            request.CustomerCode.Trim();

        var duplicate =
            await _dbContext.Customers
                .AnyAsync(
                    x => x.CustomerCode == customerCode,
                    cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A customer with this customer code already exists.");
        }

        var customer = new Customer
        {
            CustomerCode = customerCode,

            Name =
                request.Name.Trim(),

            Phone =
                NormalizeOptional(request.Phone),

            Email =
                NormalizeOptional(request.Email),

            Address =
                NormalizeOptional(request.Address),

            CreditLimit =
                request.CreditLimit,

            IsActive =
                request.IsActive
        };

        _dbContext.Customers.Add(customer);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return customer.Id;
    }

    public async Task UpdateAsync(
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        Validate(
            request.CustomerCode,
            request.Name,
            request.CreditLimit);

        var customer =
            await _dbContext.Customers
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException(
                "Customer could not be found.");
        }

        var customerCode =
            request.CustomerCode.Trim();

        var duplicate =
            await _dbContext.Customers
                .AnyAsync(
                    x =>
                        x.CustomerCode == customerCode &&
                        x.Id != request.Id,
                    cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A customer with this customer code already exists.");
        }

        customer.CustomerCode =
            customerCode;

        customer.Name =
            request.Name.Trim();

        customer.Phone =
            NormalizeOptional(request.Phone);

        customer.Email =
            NormalizeOptional(request.Email);

        customer.Address =
            NormalizeOptional(request.Address);

        customer.CreditLimit =
            request.CreditLimit;

        customer.IsActive =
            request.IsActive;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static void Validate(
        string customerCode,
        string name,
        decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
        {
            throw new InvalidOperationException(
                "Customer code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Customer name is required.");
        }

        if (creditLimit < 0)
        {
            throw new InvalidOperationException(
                "Credit limit cannot be negative.");
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}