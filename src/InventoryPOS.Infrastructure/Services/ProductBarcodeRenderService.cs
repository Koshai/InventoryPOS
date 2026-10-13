using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace InventoryPOS.Infrastructure.Services;

public sealed class ProductBarcodeRenderService
    : IProductBarcodeRenderService
{
    private readonly InventoryPosDbContext _dbContext;

    public ProductBarcodeRenderService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        IReadOnlyDictionary<Guid, ProductBarcodeRenderDto>>
        GeneratePrimaryBarcodesAsync(
            IEnumerable<Guid> productVariantIds,
            CancellationToken cancellationToken = default)
    {
        var variantIds =
            productVariantIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

        if (variantIds.Length == 0)
        {
            return new Dictionary<
                Guid,
                ProductBarcodeRenderDto>();
        }

        // One query for every product on the document.
        // Do not query separately for each quotation/invoice line.
        var barcodes =
            await _dbContext.ProductBarcodes
                .AsNoTracking()
                .Where(x =>
                    variantIds.Contains(
                        x.ProductVariantId) &&
                    x.IsPrimary)
                .Select(x => new
                {
                    x.ProductVariantId,
                    x.Value,
                    x.Type
                })
                .ToListAsync(
                    cancellationToken);

        var result =
            new Dictionary<
                Guid,
                ProductBarcodeRenderDto>();

        foreach (var barcode in barcodes)
        {
            if (string.IsNullOrWhiteSpace(
                    barcode.Value))
            {
                continue;
            }

            if (!TryResolveFormat(
        barcode.Type,
        out var barcodeFormat))
            {
                continue;
            }

            var imageBytes =
                GenerateBarcodePng(
                    barcode.Value,
                    barcodeFormat);

            result[barcode.ProductVariantId] =
                new ProductBarcodeRenderDto
                {
                    ProductVariantId =
                        barcode.ProductVariantId,

                    Value =
                        barcode.Value,

                    Format =
                        barcode.Type.ToString(),

                    ImageBytes =
                        imageBytes
                };
        }

        return result;
    }

    private static bool TryResolveFormat(
    BarcodeType type,
    out BarcodeFormat format)
    {
        switch (type)
        {
            case BarcodeType.Ean13:
                format =
                    BarcodeFormat.EAN_13;

                return true;

            case BarcodeType.UpcA:
                format =
                    BarcodeFormat.UPC_A;

                return true;

            case BarcodeType.Code128:
                format =
                    BarcodeFormat.CODE_128;

                return true;

            case BarcodeType.Other:
            default:
                format =
                    default;

                return false;
        }
    }

    private static byte[] GenerateBarcodePng(
        string value,
        BarcodeFormat format)
    {
        var writer =
            new MultiFormatWriter();

        var hints =
            new Dictionary<
                EncodeHintType,
                object>
            {
                [EncodeHintType.MARGIN] = 2
            };

        BitMatrix matrix =
            writer.encode(
                value,
                format,
                420,
                110,
                hints);

        using var bitmap =
            new SKBitmap(
                matrix.Width,
                matrix.Height);

        for (var y = 0;
             y < matrix.Height;
             y++)
        {
            for (var x = 0;
                 x < matrix.Width;
                 x++)
            {
                bitmap.SetPixel(
                    x,
                    y,
                    matrix[x, y]
                        ? SKColors.Black
                        : SKColors.White);
            }
        }

        using var image =
            SKImage.FromBitmap(
                bitmap);

        using var data =
            image.Encode(
                SKEncodedImageFormat.Png,
                100);

        return data.ToArray();
    }

    public async Task<ProductBarcodeRenderDto?>
    GenerateBarcodeAsync(
        Guid productBarcodeId,
        CancellationToken cancellationToken = default)
    {
        if (productBarcodeId == Guid.Empty)
        {
            return null;
        }

        var barcode =
            await _dbContext.ProductBarcodes
                .AsNoTracking()
                .Where(x =>
                    x.Id == productBarcodeId)
                .Select(x => new
                {
                    x.ProductVariantId,
                    x.Value,
                    x.Type
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (barcode is null ||
            string.IsNullOrWhiteSpace(
                barcode.Value))
        {
            return null;
        }

        if (!TryResolveFormat(
        barcode.Type,
        out var barcodeFormat))
        {
            return null;
        }

        var imageBytes =
            GenerateBarcodePng(
                barcode.Value,
                barcodeFormat);

        return new ProductBarcodeRenderDto
        {
            ProductVariantId =
                barcode.ProductVariantId,

            Value =
                barcode.Value,

            Format =
                barcode.Type.ToString(),

            ImageBytes =
                imageBytes
        };
    }
}