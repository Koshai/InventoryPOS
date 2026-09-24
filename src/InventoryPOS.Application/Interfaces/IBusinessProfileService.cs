using InventoryPOS.Application.DTOs.Settings;

namespace InventoryPOS.Application.Interfaces;

public interface IBusinessProfileService
{
    Task<BusinessProfileDto?> GetAsync(
        CancellationToken cancellationToken = default);

    Task<BusinessProfileDto> SaveAsync(
        BusinessProfileDto profile,
        CancellationToken cancellationToken = default);
}