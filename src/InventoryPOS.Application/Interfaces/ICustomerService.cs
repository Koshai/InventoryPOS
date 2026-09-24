using InventoryPOS.Application.DTOs.Customers;

namespace InventoryPOS.Application.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto?> GetByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);
}