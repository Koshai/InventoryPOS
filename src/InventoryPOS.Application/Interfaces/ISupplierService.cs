using InventoryPOS.Application.DTOs.Suppliers;

namespace InventoryPOS.Application.Interfaces;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<SupplierDetailsDto?> GetByIdAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default);
}