using InventoryPOS.Application.DTOs.Products;

namespace InventoryPOS.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);
}