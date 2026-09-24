using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace InventoryPOS.Desktop.Services;

public sealed class BusinessLogoService : IBusinessLogoService
{
    private const string BusinessFolderName = "Business";

    public async Task<string> SaveAsync(StorageFile sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);

        var extension =
            Path.GetExtension(sourceFile.Name).ToLowerInvariant();

        if (extension is not ".png"
            and not ".jpg"
            and not ".jpeg")
        {
            throw new InvalidOperationException(
                "Only PNG and JPEG logo files are supported.");
        }

        var localFolder =
            ApplicationData.Current.LocalFolder;

        var businessFolder =
            await localFolder.CreateFolderAsync(
                BusinessFolderName,
                CreationCollisionOption.OpenIfExists);

        // Remove an existing logo so changing extension
        // does not leave old logo files behind.
        await DeleteExistingLogoFilesAsync(businessFolder);

        var fileName = $"business-logo{extension}";

        await sourceFile.CopyAsync(
            businessFolder,
            fileName,
            NameCollisionOption.ReplaceExisting);

        // Store a relative path in PostgreSQL rather than
        // a machine-specific absolute path.
        return Path.Combine(
            BusinessFolderName,
            fileName);
    }

    public async Task RemoveAsync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var absolutePath =
            GetAbsolutePath(relativePath);

        if (string.IsNullOrWhiteSpace(absolutePath))
            return;

        try
        {
            var file =
                await StorageFile.GetFileFromPathAsync(
                    absolutePath);

            await file.DeleteAsync(
                StorageDeleteOption.PermanentDelete);
        }
        catch (FileNotFoundException)
        {
            // Already gone. Nothing else to do.
        }
        catch (UnauthorizedAccessException)
        {
            // Treat inaccessible/missing logo as removed
            // from the application's perspective.
        }
    }

    public string? GetAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        return Path.Combine(
            ApplicationData.Current.LocalFolder.Path,
            relativePath);
    }

    private static async Task DeleteExistingLogoFilesAsync(
        StorageFolder businessFolder)
    {
        var files =
            await businessFolder.GetFilesAsync();

        foreach (var file in files)
        {
            if (!file.Name.StartsWith(
                    "business-logo",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await file.DeleteAsync(
                StorageDeleteOption.PermanentDelete);
        }
    }
}