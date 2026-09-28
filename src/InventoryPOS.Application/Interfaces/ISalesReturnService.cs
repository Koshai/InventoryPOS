using InventoryPOS.Application.DTOs.SalesReturns;

namespace InventoryPOS.Application.Interfaces;

public interface ISalesReturnService
{
    Task<SalesReturnableSaleDto?> GetReturnableSaleAsync(
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<CreateSalesReturnResultDto> CreateReturnAsync(
        CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<SalesReturnHistoryResultDto> GetReturnHistoryAsync(
        SalesReturnHistoryQueryDto query,
        CancellationToken cancellationToken = default);

    Task<SalesReturnDetailsDto?> GetReturnDetailsAsync(
        Guid salesReturnId,
        CancellationToken cancellationToken = default);
}