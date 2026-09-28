using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Application.Interfaces;

public interface ISaleFinancialService
{
    Task<SaleFinancialStateDto> GetStateAsync(
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, SaleFinancialStateDto>> GetStatesAsync(
        IEnumerable<Guid> saleIds,
        CancellationToken cancellationToken = default);
}