namespace InventoryPOS.Application.Interfaces;

public interface IProductImageService
{
    Task<string> SetPrimaryImageAsync(
        Guid productId,
        string sourceFilePath,
        CancellationToken cancellationToken = default);

    Task<string?> GetPrimaryImagePathAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task RemovePrimaryImageAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}