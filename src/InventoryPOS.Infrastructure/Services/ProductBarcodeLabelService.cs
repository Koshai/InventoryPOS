using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InventoryPOS.Infrastructure.Services;

public sealed class ProductBarcodeLabelService
    : IProductBarcodeLabelService
{
    private readonly InventoryPosDbContext _dbContext;

    private readonly IProductBarcodeRenderService
        _barcodeRenderService;

    public ProductBarcodeLabelService(
        InventoryPosDbContext dbContext,
        IProductBarcodeRenderService barcodeRenderService)
    {
        _dbContext =
            dbContext;

        _barcodeRenderService =
            barcodeRenderService;
    }

    public async Task<byte[]> GenerateLabelPdfAsync(
        ProductBarcodeLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.ProductVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant is required.",
                nameof(request));
        }

        if (request.ProductBarcodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product barcode is required.",
                nameof(request));
        }

        if (request.Copies < 1 ||
            request.Copies > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Label copies must be between 1 and 500.");
        }

        // -----------------------------------------------------
        // Load authoritative product/variant/barcode information
        // -----------------------------------------------------

        var labelData =
            await (
                from barcode in _dbContext.ProductBarcodes
                    .AsNoTracking()

                join variant in _dbContext.ProductVariants
                        .AsNoTracking()
                    on barcode.ProductVariantId
                    equals variant.Id

                join product in _dbContext.Products
                        .AsNoTracking()
                    on variant.ProductId
                    equals product.Id

                where
                    barcode.Id ==
                        request.ProductBarcodeId &&
                    variant.Id ==
                        request.ProductVariantId

                select new
                {
                    ProductName =
                        product.Name,

                    VariantName =
                        variant.Name,

                    SKU =
                        variant.SKU,

                    SellingPrice =
                        variant.SellingPrice,

                    BarcodeValue =
                        barcode.Value,

                    BarcodeType =
                        barcode.Type
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (labelData is null)
        {
            throw new InvalidOperationException(
                "The selected barcode does not belong " +
                "to the selected product variant.");
        }

        // "Other" is intentionally not considered printable
        // as a physical barcode label because it has no
        // guaranteed barcode symbology.
        if (labelData.BarcodeType ==
            BarcodeType.Other)
        {
            throw new InvalidOperationException(
                "Barcodes of type 'Other' cannot be printed " +
                "as product labels. Use EAN-13, UPC-A or Code 128.");
        }

        var renderedBarcode =
            await _barcodeRenderService
                .GenerateBarcodeAsync(
                    request.ProductBarcodeId,
                    cancellationToken);

        if (renderedBarcode is null ||
            renderedBarcode.ImageBytes.Length == 0)
        {
            throw new InvalidOperationException(
                "The barcode could not be rendered.");
        }

        var productDisplayName =
            BuildProductDisplayName(
                labelData.ProductName,
                labelData.VariantName);

        // -----------------------------------------------------
        // Generate one physical 50 x 30 mm page per label.
        //
        // This is suitable for a thermal label workflow and
        // avoids treating labels as ordinary A4 documents.
        // -----------------------------------------------------

        return Document
            .Create(document =>
            {
                for (var copy = 0;
                     copy < request.Copies;
                     copy++)
                {
                    document.Page(page =>
                    {
                        page.Size(
                            50,
                            30,
                            Unit.Millimetre);

                        page.Margin(
                            2,
                            Unit.Millimetre);

                        page.DefaultTextStyle(
                            style =>
                                style
                                    .FontSize(7)
                                    .FontFamily(
                                        "Lato",
                                        "Noto Sans Bengali"));

                        page.Content()
                            .Element(container =>
                                ComposeLabel(
                                    container,
                                    productDisplayName,
                                    labelData.SKU,
                                    labelData.SellingPrice,
                                    renderedBarcode,
                                    request));
                    });
                }
            })
            .GeneratePdf();
    }

    // =========================================================
    // LABEL
    // =========================================================

    private static void ComposeLabel(
        IContainer container,
        string productName,
        string sku,
        decimal sellingPrice,
        ProductBarcodeRenderDto barcode,
        ProductBarcodeLabelRequest request)
    {
        container
            .Column(column =>
            {
                column.Spacing(1);

                // -------------------------------------------------
                // PRODUCT NAME
                // -------------------------------------------------

                column.Item()
                    .AlignCenter()
                    .Text(productName)
                    .FontSize(7.5f)
                    .SemiBold();

                // -------------------------------------------------
                // SKU + PRICE
                // -------------------------------------------------

                if (request.ShowSku ||
                    request.ShowPrice)
                {
                    column.Item()
                        .PaddingTop(1)
                        .Row(row =>
                        {
                            if (request.ShowSku)
                            {
                                row.RelativeItem()
                                    .Text(
                                        $"SKU: {sku}")
                                    .FontSize(5.5f);
                            }

                            if (request.ShowPrice)
                            {
                                row.RelativeItem()
                                    .AlignRight()
                                    .Text(
                                        $"৳{sellingPrice:N2}")
                                    .FontSize(7)
                                    .SemiBold();
                            }
                        });
                }

                // -------------------------------------------------
                // BARCODE GRAPHIC
                // -------------------------------------------------

                column.Item()
                    .PaddingTop(1)
                    .AlignCenter()
                    .Height(
                        10,
                        Unit.Millimetre)
                    .Width(
                        43,
                        Unit.Millimetre)
                    .Image(
                        barcode.ImageBytes)
                    .FitArea();

                // -------------------------------------------------
                // HUMAN-READABLE VALUE
                // -------------------------------------------------

                column.Item()
                    .PaddingTop(0.5f)
                    .AlignCenter()
                    .Text(
                        barcode.Value)
                    .FontSize(6)
                    .SemiBold();
            });
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static string BuildProductDisplayName(
        string productName,
        string? variantName)
    {
        if (string.IsNullOrWhiteSpace(
                variantName))
        {
            return productName;
        }

        return $"{productName} - {variantName}";
    }
}