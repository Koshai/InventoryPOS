using InventoryPOS.Application.DTOs.Sales;

namespace InventoryPOS.Application.Interfaces;

public interface IInvoicePdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(
    SaleDetailsDto sale,
    CancellationToken cancellationToken = default);
}