using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly InventoryPosDbContext _dbContext;

    public ProductService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductListItemDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                NameBn = x.NameBn,
                CategoryName = x.Category!.Name,

                BrandName = x.Brand != null
                    ? x.Brand.Name
                    : null,

                UnitSymbol = x.Unit!.Symbol,

                SKU = x.Variants
                    .OrderBy(v => v.Id)
                    .Select(v => v.SKU)
                    .FirstOrDefault() ?? string.Empty,

                SellingPrice = x.Variants
                    .OrderBy(v => v.Id)
                    .Select(v => v.SellingPrice)
                    .FirstOrDefault(),

                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductDetailsDto
            {
                Id = x.Id,
                Name = x.Name,
                NameBn = x.NameBn,
                Description = x.Description,

                CategoryId = x.CategoryId,
                CategoryName = x.Category!.Name,

                BrandId = x.BrandId,
                BrandName = x.Brand != null
                    ? x.Brand.Name
                    : null,

                UnitId = x.UnitId,
                UnitName = x.Unit!.Name,

                IsActive = x.IsActive,

                Variants = x.Variants
                    .OrderBy(v => v.Id)
                    .Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        SKU = v.SKU,
                        Name = v.Name,
                        NameBn = v.NameBn,
                        CostPrice = v.CostPrice,
                        SellingPrice = v.SellingPrice,
                        ReorderLevel = v.ReorderLevel,
                        IsActive = v.IsActive
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProductValues(
            request.Name,
            request.SKU,
            request.CostPrice,
            request.SellingPrice,
            request.ReorderLevel);

        var sku =
            request.SKU.Trim();

        var skuExists =
            await _dbContext.ProductVariants
                .AnyAsync(
                    x => x.SKU == sku,
                    cancellationToken);

        if (skuExists)
        {
            throw new InvalidOperationException(
                $"SKU '{sku}' already exists.");
        }

        await ValidateLookupsAsync(
            request.CategoryId,
            request.BrandId,
            request.UnitId,
            cancellationToken);

        var product = new Product
        {
            Name = request.Name.Trim(),

            NameBn =
                NormalizeOptional(request.NameBn),

            Description =
                NormalizeOptional(request.Description),

            CategoryId =
                request.CategoryId,

            BrandId =
                request.BrandId,

            UnitId =
                request.UnitId,

            IsActive =
                request.IsActive
        };

        product.Variants.Add(
            new ProductVariant
            {
                SKU = sku,

                CostPrice =
                    request.CostPrice,

                SellingPrice =
                    request.SellingPrice,

                ReorderLevel =
                    request.ReorderLevel,

                IsActive = true
            });

        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return product.Id;
    }

    public async Task UpdateAsync(
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProductValues(
            request.Name,
            request.SKU,
            request.CostPrice,
            request.SellingPrice,
            request.ReorderLevel);

        var product =
            await _dbContext.Products
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException(
                "Product could not be found.");
        }

        await ValidateLookupsAsync(
            request.CategoryId,
            request.BrandId,
            request.UnitId,
            cancellationToken);

        /*
         * For the current MVP, the Edit Product screen edits
         * one primary/default variant.
         *
         * If VariantId is supplied, use that exact variant.
         * Otherwise fall back to the first variant belonging
         * to the product.
         */
        ProductVariant? variant = null;

        if (request.VariantId.HasValue)
        {
            variant =
                product.Variants.FirstOrDefault(
                    x => x.Id == request.VariantId.Value);
        }
        else
        {
            variant =
                product.Variants
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();
        }

        if (variant is null)
        {
            throw new InvalidOperationException(
                "Product variant could not be found.");
        }

        var sku =
            request.SKU.Trim();

        var duplicateSku =
            await _dbContext.ProductVariants
                .AnyAsync(
                    x =>
                        x.SKU == sku &&
                        x.Id != variant.Id,
                    cancellationToken);

        if (duplicateSku)
        {
            throw new InvalidOperationException(
                $"SKU '{sku}' already exists.");
        }

        // -----------------------------------------
        // Update Product
        // -----------------------------------------

        product.Name =
            request.Name.Trim();

        product.NameBn =
            NormalizeOptional(
                request.NameBn);

        product.Description =
            NormalizeOptional(
                request.Description);

        product.CategoryId =
            request.CategoryId;

        product.BrandId =
            request.BrandId;

        product.UnitId =
            request.UnitId;

        product.IsActive =
            request.IsActive;

        // -----------------------------------------
        // Update primary ProductVariant
        // -----------------------------------------

        variant.SKU =
            sku;

        variant.CostPrice =
            request.CostPrice;

        variant.SellingPrice =
            request.SellingPrice;

        variant.ReorderLevel =
            request.ReorderLevel;

        variant.IsActive =
            request.VariantIsActive;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task ValidateLookupsAsync(
        Guid categoryId,
        Guid? brandId,
        Guid unitId,
        CancellationToken cancellationToken)
    {
        var categoryExists =
            await _dbContext.Categories
                .AnyAsync(
                    x =>
                        x.Id == categoryId &&
                        x.IsActive,
                    cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException(
                "The selected category is invalid.");
        }

        var unitExists =
            await _dbContext.Units
                .AnyAsync(
                    x =>
                        x.Id == unitId &&
                        x.IsActive,
                    cancellationToken);

        if (!unitExists)
        {
            throw new InvalidOperationException(
                "The selected unit is invalid.");
        }

        if (brandId.HasValue)
        {
            var brandExists =
                await _dbContext.Brands
                    .AnyAsync(
                        x =>
                            x.Id == brandId.Value &&
                            x.IsActive,
                        cancellationToken);

            if (!brandExists)
            {
                throw new InvalidOperationException(
                    "The selected brand is invalid.");
            }
        }
    }

    private static void ValidateProductValues(
        string name,
        string sku,
        decimal costPrice,
        decimal sellingPrice,
        decimal reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException(
                "SKU is required.");
        }

        if (costPrice < 0)
        {
            throw new ArgumentException(
                "Cost price cannot be negative.");
        }

        if (sellingPrice <= 0)
        {
            throw new ArgumentException(
                "Selling price must be greater than zero.");
        }

        if (reorderLevel < 0)
        {
            throw new ArgumentException(
                "Reorder level cannot be negative.");
        }
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}