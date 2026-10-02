using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Application.Interfaces;

public interface ISalesQuoteService
{
    Task<SalesQuoteResultDto> CreateAsync(
        CreateSalesQuoteRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesQuoteHistoryDto>>
        GetHistoryAsync(
            string? search = null,
            CancellationToken cancellationToken = default);

    Task<SalesQuoteDetailsDto?> GetByIdAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default);

    Task IssueAsync(
    Guid salesQuoteId,
    CancellationToken cancellationToken = default);

    Task AcceptAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default);

    Task ExpireAsync(
        Guid salesQuoteId,
        CancellationToken cancellationToken = default);
}