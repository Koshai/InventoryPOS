
using InventoryPOS.Application.DTOs.WarrantyClaims;

namespace InventoryPOS.Application.Interfaces;

public interface IWarrantyClaimQueryService
{
    Task<WarrantyClaimPageDto> SearchAsync(
        string? searchText,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    Task<WarrantyClaimDetailsDto?> GetDetailsAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);
}
