using System.Threading.Tasks;
using Windows.Storage;

namespace InventoryPOS.Desktop.Services;

public interface IBusinessLogoService
{
    Task<string> SaveAsync(StorageFile sourceFile);

    Task RemoveAsync(string? relativePath);

    string? GetAbsolutePath(string? relativePath);
}