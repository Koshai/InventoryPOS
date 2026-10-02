
using InventoryPOS.Application.DTOs.Warranty;

namespace InventoryPOS.Application.Interfaces;

public interface IWarrantyClaimService
{
    Task<RegisterWarrantyClaimResultDto> RegisterAsync(
        RegisterWarrantyClaimRequest request,
        CancellationToken cancellationToken = default);
}
