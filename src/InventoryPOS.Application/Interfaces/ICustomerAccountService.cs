using InventoryPOS.Application.DTOs.Customers;

namespace InventoryPOS.Application.Interfaces;

public interface ICustomerAccountService
{
    Task<CustomerAccountDto?> GetAccountAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerPaymentResultDto> ReceiveInvoicePaymentAsync(
        Guid saleId,
        ReceiveCustomerPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerPaymentResultDto> ReceiveAccountPaymentAsync(
        ReceiveCustomerPaymentRequest request,
        CancellationToken cancellationToken = default);
}