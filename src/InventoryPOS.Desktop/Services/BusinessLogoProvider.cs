using InventoryPOS.Application.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace InventoryPOS.Desktop.Services;

public sealed class BusinessLogoProvider
    : IBusinessLogoProvider
{
    public async Task<byte[]?> GetLogoAsync(
        string? logoPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return null;

        try
        {
            var absolutePath = Path.Combine(
                ApplicationData.Current.LocalFolder.Path,
                logoPath);

            if (!File.Exists(absolutePath))
                return null;

            return await File.ReadAllBytesAsync(
                absolutePath,
                cancellationToken);
        }
        catch
        {
            // A missing/corrupt logo should never prevent
            // an invoice from being generated.
            return null;
        }
    }
}