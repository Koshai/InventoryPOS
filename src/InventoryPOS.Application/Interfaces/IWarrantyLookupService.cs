
using InventoryPOS.Application.DTOs.Warranty;

namespace InventoryPOS.Application.Interfaces;

public interface IWarrantyLookupService
{
    Task<WarrantyLookupResultDto?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    Task<WarrantyLookupResultDto?> GetBySaleIdAsync(
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task<WarrantyInvoiceSearchPageDto> SearchInvoicesAsync(
        string? searchText = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);
}
