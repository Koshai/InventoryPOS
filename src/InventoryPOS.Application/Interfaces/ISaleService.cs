using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Application.Interfaces;

public interface ISaleService
{
    Task<IReadOnlyList<PosProductDto>> GetProductsAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosCustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationLookupDto>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<SaleResultDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SaleResultDto?> FindCommittedCheckoutAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SalesHistoryResultDto> GetSalesHistoryAsync(SalesHistoryQuery query, CancellationToken cancellationToken = default);
    Task<SaleDetailsDto?> GetSaleDetailsAsync(Guid saleId, CancellationToken cancellationToken = default);
}
