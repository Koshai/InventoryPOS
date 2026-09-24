using InventoryPOS.Application.DTOs.Suppliers;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly InventoryPosDbContext _dbContext;

    public SupplierService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SupplierListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SupplierListItemDto
            {
                Id = x.Id,
                SupplierCode = x.SupplierCode,
                Name = x.Name,
                ContactPerson = x.ContactPerson,
                Phone = x.Phone,
                Email = x.Email,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierDetailsDto?> GetByIdAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.Id == supplierId)
            .Select(x => new SupplierDetailsDto
            {
                Id = x.Id,
                SupplierCode = x.SupplierCode,
                Name = x.Name,
                ContactPerson = x.ContactPerson,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                TaxNumber = x.TaxNumber,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.SupplierCode, request.Name);

        var normalizedCode =
            request.SupplierCode.Trim();

        var codeExists =
            await _dbContext.Suppliers
                .AnyAsync(
                    x => x.SupplierCode == normalizedCode,
                    cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException(
                "A supplier with this supplier code already exists.");
        }

        var supplier = new Supplier
        {
            SupplierCode = normalizedCode,
            Name = request.Name.Trim(),
            ContactPerson = Clean(request.ContactPerson),
            Phone = Clean(request.Phone),
            Email = Clean(request.Email),
            Address = Clean(request.Address),
            TaxNumber = Clean(request.TaxNumber),
            IsActive = request.IsActive
        };

        _dbContext.Suppliers.Add(supplier);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return supplier.Id;
    }

    public async Task UpdateAsync(
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Supplier ID is required.");
        }

        Validate(request.SupplierCode, request.Name);

        var supplier =
            await _dbContext.Suppliers
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

        if (supplier is null)
        {
            throw new InvalidOperationException(
                "Supplier could not be found.");
        }

        var normalizedCode =
            request.SupplierCode.Trim();

        var duplicate =
            await _dbContext.Suppliers
                .AnyAsync(
                    x =>
                        x.Id != request.Id &&
                        x.SupplierCode == normalizedCode,
                    cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A supplier with this supplier code already exists.");
        }

        supplier.SupplierCode = normalizedCode;
        supplier.Name = request.Name.Trim();
        supplier.ContactPerson = Clean(request.ContactPerson);
        supplier.Phone = Clean(request.Phone);
        supplier.Email = Clean(request.Email);
        supplier.Address = Clean(request.Address);
        supplier.TaxNumber = Clean(request.TaxNumber);
        supplier.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static void Validate(
        string supplierCode,
        string name)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            throw new InvalidOperationException(
                "Supplier code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                "Supplier name is required.");
        }
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}