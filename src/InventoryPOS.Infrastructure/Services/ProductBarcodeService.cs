using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class ProductBarcodeService
    : IProductBarcodeService
{
    private readonly InventoryPosDbContext _db;

    public ProductBarcodeService(
        InventoryPosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductBarcodeDto>>
        GetForVariantAsync(
            Guid productVariantId,
            CancellationToken cancellationToken = default)
    {
        return await _db.ProductBarcodes
            .AsNoTracking()
            .Where(x =>
                x.ProductVariantId ==
                productVariantId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Value)
            .Select(x =>
                new ProductBarcodeDto
                {
                    Id = x.Id,
                    ProductVariantId =
                        x.ProductVariantId,
                    Value = x.Value,
                    Type = x.Type,
                    IsPrimary = x.IsPrimary
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductBarcodeLookupDto?>
        ResolveAsync(
            string barcode,
            CancellationToken cancellationToken = default)
    {
        var normalized =
            Normalize(barcode);

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return await _db.ProductBarcodes
            .AsNoTracking()
            .Where(x =>
                x.Value == normalized)
            .Select(x =>
                new ProductBarcodeLookupDto
                {
                    ProductVariantId =
                        x.ProductVariantId,
                    Barcode =
                        x.Value,
                    BarcodeType =
                        x.Type
                })
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<ProductBarcodeDto>
    AddAsync(
        Guid productVariantId,
        string barcode,
        BarcodeType type,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid product variant is required.");
        }

        var normalized =
            Normalize(barcode);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                "Barcode is required.");
        }

        ValidateBarcode(
            normalized,
            type);

        var variantExists =
            await _db.ProductVariants
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == productVariantId,
                    cancellationToken);

        if (!variantExists)
        {
            throw new InvalidOperationException(
                "Product variant was not found.");
        }

        var duplicate =
            await _db.ProductBarcodes
                .AsNoTracking()
                .AnyAsync(
                    x => x.Value == normalized,
                    cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "This barcode is already assigned " +
                "to a product variant.");
        }

        // ---------------------------------------------------------
        // PRIMARY RULE
        //
        // The first barcode for a variant automatically becomes
        // primary even if the UI did not explicitly request it.
        // ---------------------------------------------------------

        var hasExistingBarcode =
            await _db.ProductBarcodes
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ProductVariantId ==
                        productVariantId,
                    cancellationToken);

        var makePrimary =
            isPrimary ||
            !hasExistingBarcode;

        if (makePrimary)
        {
            var existingPrimary =
                await _db.ProductBarcodes
                    .Where(x =>
                        x.ProductVariantId ==
                            productVariantId &&
                        x.IsPrimary)
                    .ToListAsync(
                        cancellationToken);

            foreach (var item in existingPrimary)
            {
                item.IsPrimary =
                    false;
            }
        }

        var entity =
            new ProductBarcode
            {
                ProductVariantId =
                    productVariantId,

                Value =
                    normalized,

                Type =
                    type,

                IsPrimary =
                    makePrimary
            };

        _db.ProductBarcodes.Add(
            entity);

        await _db.SaveChangesAsync(
            cancellationToken);

        return new ProductBarcodeDto
        {
            Id =
                entity.Id,

            ProductVariantId =
                entity.ProductVariantId,

            Value =
                entity.Value,

            Type =
                entity.Type,

            IsPrimary =
                entity.IsPrimary
        };
    }

    public async Task DeleteAsync(
    Guid barcodeId,
    CancellationToken cancellationToken = default)
    {
        if (barcodeId == Guid.Empty)
        {
            return;
        }

        var entity =
            await _db.ProductBarcodes
                .SingleOrDefaultAsync(
                    x => x.Id == barcodeId,
                    cancellationToken);

        if (entity is null)
        {
            return;
        }

        var variantId =
            entity.ProductVariantId;

        var wasPrimary =
            entity.IsPrimary;

        _db.ProductBarcodes.Remove(
            entity);

        // ---------------------------------------------------------
        // If the primary barcode is removed, promote another
        // barcode belonging to the same variant.
        // ---------------------------------------------------------

        if (wasPrimary)
        {
            var replacement =
                await _db.ProductBarcodes
                    .Where(x =>
                        x.ProductVariantId ==
                            variantId &&
                        x.Id != barcodeId)
                    .OrderBy(x => x.Value)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (replacement is not null)
            {
                replacement.IsPrimary =
                    true;
            }
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    private static string Normalize(
        string? barcode)
    {
        return barcode?
            .Trim() ??
            string.Empty;
    }

    private static void ValidateBarcode(
    string barcode,
    BarcodeType type)
    {
        switch (type)
        {
            case BarcodeType.Ean13:
                ValidateNumericBarcode(
                    barcode,
                    13,
                    "EAN-13");
                break;

            case BarcodeType.UpcA:
                ValidateNumericBarcode(
                    barcode,
                    12,
                    "UPC-A");
                break;

            case BarcodeType.Code128:
                if (barcode.Length > 100)
                {
                    throw new InvalidOperationException(
                        "Code 128 barcode cannot exceed 100 characters.");
                }

                break;

            case BarcodeType.Other:
                if (barcode.Length > 100)
                {
                    throw new InvalidOperationException(
                        "Barcode cannot exceed 100 characters.");
                }

                break;

            default:
                throw new InvalidOperationException(
                    "Unsupported barcode type.");
        }
    }

    private static void ValidateNumericBarcode(
        string barcode,
        int requiredLength,
        string displayName)
    {
        if (barcode.Length != requiredLength)
        {
            throw new InvalidOperationException(
                $"{displayName} must contain exactly " +
                $"{requiredLength} digits.");
        }

        if (!barcode.All(char.IsDigit))
        {
            throw new InvalidOperationException(
                $"{displayName} must contain digits only.");
        }

        if (!HasValidCheckDigit(barcode))
        {
            throw new InvalidOperationException(
                $"{displayName} has an invalid check digit.");
        }
    }

    private static bool HasValidCheckDigit(
    string barcode)
    {
        var sum = 0;

        // Start from the rightmost data digit,
        // immediately before the check digit.
        var weight = 3;

        for (var i = barcode.Length - 2;
             i >= 0;
             i--)
        {
            var digit =
                barcode[i] - '0';

            sum += digit * weight;

            weight =
                weight == 3
                    ? 1
                    : 3;
        }

        var expectedCheckDigit =
            (10 - (sum % 10)) % 10;

        var actualCheckDigit =
            barcode[^1] - '0';

        return
            expectedCheckDigit ==
            actualCheckDigit;
    }
}