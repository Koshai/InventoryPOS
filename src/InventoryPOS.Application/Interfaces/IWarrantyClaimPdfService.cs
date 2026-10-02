using InventoryPOS.Application.DTOs.WarrantyClaims;

namespace InventoryPOS.Application.Interfaces;

public interface IWarrantyClaimPdfService
{
    Task<byte[]> GenerateWarrantyClaimPdfAsync(
        WarrantyClaimDetailsDto claim,
        CancellationToken cancellationToken = default);
}