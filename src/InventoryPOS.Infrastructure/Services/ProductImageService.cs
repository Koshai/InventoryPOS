using InventoryPOS.Application.Licensing;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InventoryPOS.Infrastructure.Services;

public class ProductImageService : IProductImageService
{
    private readonly InventoryPosDbContext _dbContext;
    private readonly string _dataRoot;

    private readonly ILicenseAccessGuard _licenseAccessGuard;


    public ProductImageService(
        InventoryPosDbContext dbContext,
        IConfiguration configuration,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _licenseAccessGuard = licenseAccessGuard ?? throw new ArgumentNullException(nameof(licenseAccessGuard));
        _dbContext = dbContext;

        var configuredRoot =
            configuration["Storage:DataRoot"];

        _dataRoot =
            string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "InventoryPOS",
                    "Data")
                : configuredRoot;
    }

    public async Task<string> SetPrimaryImageAsync(
        Guid productId,
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException(
                "The selected image could not be found.",
                sourceFilePath);
        }

        var productExists =
            await _dbContext.Products.AnyAsync(
                x => x.Id == productId,
                cancellationToken);

        if (!productExists)
        {
            throw new InvalidOperationException(
                "Product could not be found.");
        }

        var extension =
            Path.GetExtension(sourceFilePath)
                .ToLowerInvariant();

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "Only JPG, PNG and WebP product images are supported.");
        }

        var productFolder =
            Path.Combine(
                _dataRoot,
                "Images",
                "Products",
                productId.ToString());

        Directory.CreateDirectory(productFolder);

        var fileName =
            $"primary{extension}";

        var destinationPath =
            Path.Combine(
                productFolder,
                fileName);

        var existingImage =
            await _dbContext.ProductImages
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductId == productId &&
                        x.IsPrimary,
                    cancellationToken);

        // Remove an old primary file if its extension changed.
        if (existingImage is not null)
        {
            var oldAbsolutePath =
                GetAbsolutePath(
                    existingImage.RelativePath);

            if (File.Exists(oldAbsolutePath) &&
                !string.Equals(
                    oldAbsolutePath,
                    destinationPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(oldAbsolutePath);
            }
        }

        File.Copy(
            sourceFilePath,
            destinationPath,
            overwrite: true);

        var relativePath =
            Path.GetRelativePath(
                _dataRoot,
                destinationPath);

        if (existingImage is null)
        {
            existingImage = new ProductImage
            {
                ProductId = productId,
                FileName = fileName,
                RelativePath = relativePath,
                IsPrimary = true,
                SortOrder = 0
            };

            _dbContext.ProductImages.Add(
                existingImage);
        }
        else
        {
            existingImage.FileName =
                fileName;

            existingImage.RelativePath =
                relativePath;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return destinationPath;
    }

    public async Task<string?> GetPrimaryImagePathAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var relativePath =
            await _dbContext.ProductImages
                .AsNoTracking()
                .Where(x =>
                    x.ProductId == productId &&
                    x.IsPrimary)
                .Select(x => x.RelativePath)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
            relativePath))
        {
            return null;
        }

        var absolutePath =
            GetAbsolutePath(relativePath);

        return File.Exists(absolutePath)
            ? absolutePath
            : null;
    }

    public async Task RemovePrimaryImageAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        var image =
            await _dbContext.ProductImages
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductId == productId &&
                        x.IsPrimary,
                    cancellationToken);

        if (image is null)
            return;

        var absolutePath =
            GetAbsolutePath(
                image.RelativePath);

        _dbContext.ProductImages.Remove(
            image);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    private string GetAbsolutePath(
        string relativePath)
    {
        return Path.Combine(
            _dataRoot,
            relativePath);
    }
}