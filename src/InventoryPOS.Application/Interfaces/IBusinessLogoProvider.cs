namespace InventoryPOS.Application.Interfaces;

public interface IBusinessLogoProvider
{
    Task<byte[]?> GetLogoAsync(
        string? logoPath,
        CancellationToken cancellationToken = default);
}