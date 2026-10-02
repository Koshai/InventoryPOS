using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Application.Interfaces;

public interface ISalesQuotePdfService
{
    Task<byte[]> GenerateQuotationPdfAsync(
        SalesQuoteDetailsDto quote,
        CancellationToken cancellationToken = default);
}