using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.DTOs.Settings;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryPOS.Infrastructure.Services;

public class SalesQuotePdfService : ISalesQuotePdfService
{
    private const string DefaultAccentColor = "#4F56D9";
    private const string SoftBackground = "#F4F5FB";
    private const string BorderColor = "#E1E3EA";
    private const string MutedText = "#747784";

    private readonly IBusinessProfileService
        _businessProfileService;

    private readonly IBusinessLogoProvider
        _businessLogoProvider;

    private readonly IDocumentQrService
        _documentQrService;

    private readonly IProductBarcodeRenderService
        _productBarcodeRenderService;

    public SalesQuotePdfService(
        IBusinessProfileService businessProfileService,
        IBusinessLogoProvider businessLogoProvider,
        IDocumentQrService documentQrService,
        IProductBarcodeRenderService productBarcodeRenderService)
    {
        _businessProfileService =
            businessProfileService;

        _businessLogoProvider =
            businessLogoProvider;

        _documentQrService =
            documentQrService;

        _productBarcodeRenderService =
            productBarcodeRenderService;
    }

    public async Task<byte[]> GenerateQuotationPdfAsync(
        SalesQuoteDetailsDto quote,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var business =
            await _businessProfileService.GetAsync(
                cancellationToken);

        byte[]? logoBytes = null;

        if (!string.IsNullOrWhiteSpace(
                business?.LogoPath))
        {
            logoBytes =
                await _businessLogoProvider.GetLogoAsync(
                    business.LogoPath,
                    cancellationToken);
        }

        var accent =
            GetAccentColor(business);

        // ---------------------------------------------------------
        // DOCUMENT QR
        // ---------------------------------------------------------

        var documentQr =
            await _documentQrService.GenerateAsync(
                DocumentType.SalesQuote,
                quote.Id,
                cancellationToken);

        var qrBytes =
            documentQr.PngBytes;

        // ---------------------------------------------------------
        // PRODUCT BARCODES
        // ---------------------------------------------------------

        var productBarcodes =
            await _productBarcodeRenderService
                .GeneratePrimaryBarcodesAsync(
                    quote.Lines.Select(
                        x => x.ProductVariantId),
                    cancellationToken);

        return Document
            .Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.MarginHorizontal(30);
                    page.MarginTop(25);
                    page.MarginBottom(25);

                    page.DefaultTextStyle(
                        style => style
                            .FontSize(9)
                            .FontFamily(
                                "Lato",
                                "Noto Sans Bengali"));

                    page.Header()
                        .Element(container =>
                            ComposeHeader(
                                container,
                                quote,
                                business,
                                logoBytes,
                                qrBytes,
                                accent));

                    page.Content()
                        .PaddingTop(12)
                        .Element(container =>
                            ComposeContent(
                                container,
                                quote,
                                business,
                                productBarcodes,
                                accent));

                    page.Footer()
                        .Element(container =>
                            ComposeFooter(
                                container,
                                business,
                                accent));
                });
            })
            .GeneratePdf();
    }

    // =========================================================
    // HEADER
    // =========================================================

    private static void ComposeHeader(
        IContainer container,
        SalesQuoteDetailsDto quote,
        BusinessProfileDto? business,
        byte[]? logoBytes,
        byte[] qrBytes,
        string accent)
    {
        container.Column(column =>
        {
            column.Item()
                .Row(row =>
                {
                    // -------------------------------------------------
                    // LOGO
                    // -------------------------------------------------

                    row.ConstantItem(85)
                        .Height(65)
                        .Element(logo =>
                        {
                            if (logoBytes is { Length: > 0 })
                            {
                                logo
                                    .AlignLeft()
                                    .AlignMiddle()
                                    .Image(logoBytes)
                                    .FitArea();
                            }
                        });

                    // -------------------------------------------------
                    // BUSINESS IDENTITY
                    // -------------------------------------------------

                    row.RelativeItem()
                        .PaddingLeft(8)
                        .Column(identity =>
                        {
                            identity.Item()
                                .Text(
                                    business?.BusinessName ??
                                    "InventoryPOS")
                                .FontSize(17)
                                .SemiBold()
                                .FontColor(accent);

                            if (!string.IsNullOrWhiteSpace(
                                    business?.BusinessNameBn))
                            {
                                identity.Item()
                                    .PaddingTop(1)
                                    .Text(
                                        business.BusinessNameBn)
                                    .FontSize(10)
                                    .SemiBold();
                            }

                            if (!string.IsNullOrWhiteSpace(
                                    business?.Address))
                            {
                                identity.Item()
                                    .PaddingTop(3)
                                    .Text(
                                        business.Address)
                                    .FontSize(7.5f)
                                    .FontColor(MutedText);
                            }

                            if (!string.IsNullOrWhiteSpace(
                                    business?.AddressBn))
                            {
                                identity.Item()
                                    .PaddingTop(1)
                                    .Text(
                                        business.AddressBn)
                                    .FontSize(7.5f)
                                    .FontColor(MutedText);
                            }

                            if (business is not null)
                            {
                                var contact =
                                    BuildContactLine(
                                        business);

                                if (!string.IsNullOrWhiteSpace(
                                        contact))
                                {
                                    identity.Item()
                                        .PaddingTop(2)
                                        .Text(contact)
                                        .FontSize(7.5f)
                                        .FontColor(MutedText);
                                }
                            }
                        });

                    // -------------------------------------------------
                    // QUOTATION META + DOCUMENT QR
                    // -------------------------------------------------

                    row.ConstantItem(205)
                        .Row(right =>
                        {
                            right.RelativeItem()
                                .PaddingRight(8)
                                .Column(meta =>
                                {
                                    meta.Item()
                                        .AlignRight()
                                        .Text("QUOTATION")
                                        .FontSize(17)
                                        .SemiBold()
                                        .FontColor(accent);

                                    meta.Item()
                                        .PaddingTop(1)
                                        .AlignRight()
                                        .Text("মূল্য প্রস্তাব")
                                        .FontSize(10)
                                        .SemiBold();

                                    meta.Item()
                                        .PaddingTop(7)
                                        .AlignRight()
                                        .Text(
                                            quote.QuoteNumber)
                                        .FontSize(10)
                                        .SemiBold();

                                    meta.Item()
                                        .PaddingTop(2)
                                        .AlignRight()
                                        .Text(
                                            $"Status: " +
                                            $"{GetStatusDisplay(quote)}")
                                        .FontSize(7.5f)
                                        .FontColor(MutedText);
                                });

                            right.ConstantItem(58)
                                .Column(qr =>
                                {
                                    qr.Item()
                                        .Width(54)
                                        .Height(54)
                                        .Image(qrBytes)
                                        .FitArea();

                                    qr.Item()
                                        .PaddingTop(2)
                                        .AlignCenter()
                                        .Text("Document QR")
                                        .FontSize(5.5f)
                                        .FontColor(MutedText);
                                });
                        });
                });

            column.Item()
                .PaddingTop(10)
                .LineHorizontal(1)
                .LineColor(accent);

            column.Item()
                .PaddingTop(9)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(text =>
                        {
                            text.Span(
                                    "Date / তারিখ: ")
                                .FontColor(accent)
                                .SemiBold();

                            text.Span(
                                quote.QuoteDate
                                    .ToLocalTime()
                                    .ToString(
                                        "dd MMM yyyy"));
                        });

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span(
                                    "Valid Until / বৈধ: ")
                                .FontColor(accent)
                                .SemiBold();

                            text.Span(
                                FormatBusinessDate(
                                    quote.ValidUntilDate));
                        });
                });
        });
    }

    // =========================================================
    // CONTENT
    // =========================================================

    private static void ComposeContent(
        IContainer container,
        SalesQuoteDetailsDto quote,
        BusinessProfileDto? business,
        IReadOnlyDictionary<Guid, ProductBarcodeRenderDto>
            productBarcodes,
        string accent)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item()
                .Element(c =>
                    ComposeParties(
                        c,
                        quote,
                        business,
                        accent));

            column.Item()
                .Element(c =>
                    ComposeItemsTable(
                        c,
                        quote,
                        productBarcodes,
                        accent));

            column.Item()
                .Element(c =>
                    ComposeFinancialArea(
                        c,
                        quote,
                        accent));

            if (!string.IsNullOrWhiteSpace(
                    quote.Notes))
            {
                column.Item()
                    .Element(c =>
                        ComposeTextSection(
                            c,
                            "NOTES / নোট",
                            quote.Notes,
                            accent));
            }

            if (!string.IsNullOrWhiteSpace(
                    quote.Terms))
            {
                column.Item()
                    .Element(c =>
                        ComposeTextSection(
                            c,
                            "TERMS / শর্তাবলী",
                            quote.Terms,
                            accent));
            }

            column.Item()
                .PaddingTop(3)
                .Background(SoftBackground)
                .Padding(10)
                .Text(
                    "This quotation is provided for pricing and " +
                    "proposal purposes only. It is not a sales invoice.")
                .FontSize(7.5f)
                .FontColor(MutedText);

            column.Item()
                .PaddingTop(14)
                .Element(c =>
                    ComposeSignatures(
                        c,
                        accent));
        });
    }

    // =========================================================
    // CUSTOMER + BUSINESS
    // =========================================================

    private static void ComposeParties(
        IContainer container,
        SalesQuoteDetailsDto quote,
        BusinessProfileDto? business,
        string accent)
    {
        container
            .PaddingVertical(4)
            .Row(row =>
            {
                // -------------------------------------------------
                // CUSTOMER
                // -------------------------------------------------

                row.RelativeItem()
                    .PaddingRight(20)
                    .Column(column =>
                    {
                        SectionLabel(
                            column,
                            "QUOTATION FOR / ক্রেতা",
                            accent);

                        column.Item()
                            .PaddingTop(5)
                            .Text(
                                quote.CustomerName)
                            .FontSize(11)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(
                                quote.CustomerPhone))
                        {
                            AddSmallText(
                                column,
                                $"Phone: {quote.CustomerPhone}");
                        }

                        if (!string.IsNullOrWhiteSpace(
                                quote.CustomerEmail))
                        {
                            AddSmallText(
                                column,
                                $"Email: {quote.CustomerEmail}");
                        }
                    });

                // -------------------------------------------------
                // BUSINESS
                // -------------------------------------------------

                row.RelativeItem()
                    .AlignRight()
                    .Column(column =>
                    {
                        column.Item()
                            .AlignRight()
                            .Text(
                                "QUOTED BY / প্রস্তাবকারী")
                            .FontSize(8)
                            .SemiBold()
                            .FontColor(accent);

                        column.Item()
                            .PaddingTop(5)
                            .AlignRight()
                            .Text(
                                business?.BusinessName ??
                                "InventoryPOS")
                            .FontSize(11)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(
                                business?.BusinessNameBn))
                        {
                            column.Item()
                                .PaddingTop(1)
                                .AlignRight()
                                .Text(
                                    business.BusinessNameBn)
                                .FontSize(8);
                        }

                        column.Item()
                            .PaddingTop(2)
                            .AlignRight()
                            .Text(
                                quote.LocationName)
                            .FontSize(8)
                            .FontColor(MutedText);

                        if (!string.IsNullOrWhiteSpace(
                                business?.Phone))
                        {
                            column.Item()
                                .PaddingTop(1)
                                .AlignRight()
                                .Text(
                                    business.Phone)
                                .FontSize(8)
                                .FontColor(MutedText);
                        }

                        if (business is not null)
                        {
                            var registration =
                                BuildRegistrationLine(
                                    business);

                            if (!string.IsNullOrWhiteSpace(
                                    registration))
                            {
                                column.Item()
                                    .PaddingTop(2)
                                    .AlignRight()
                                    .Text(
                                        registration)
                                    .FontSize(7)
                                    .FontColor(MutedText);
                            }
                        }
                    });
            });
    }

    // =========================================================
    // ITEMS TABLE
    // =========================================================

    private static void ComposeItemsTable(
        IContainer container,
        SalesQuoteDetailsDto quote,
        IReadOnlyDictionary<Guid, ProductBarcodeRenderDto>
            productBarcodes,
        string accent)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(0.65f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(0.7f);
                columns.RelativeColumn(1.05f);
            });

            table.Header(header =>
            {
                HeaderCell(
                    header.Cell(),
                    "SL.",
                    accent);

                HeaderCell(
                    header.Cell(),
                    "PRODUCT / পণ্য",
                    accent);

                HeaderCell(
                    header.Cell(),
                    "SKU",
                    accent);

                HeaderCell(
                    header.Cell(),
                    "QTY",
                    accent,
                    true);

                HeaderCell(
                    header.Cell(),
                    "PRICE",
                    accent,
                    true);

                HeaderCell(
                    header.Cell(),
                    "DISC.",
                    accent,
                    true);

                HeaderCell(
                    header.Cell(),
                    "TAX",
                    accent,
                    true);

                HeaderCell(
                    header.Cell(),
                    "TOTAL",
                    accent,
                    true);
            });

            var index = 1;

            foreach (var line in quote.Lines)
            {
                var alternate =
                    index % 2 == 0;

                // -------------------------------------------------
                // SL
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .Text(
                        index.ToString("00"));

                // -------------------------------------------------
                // PRODUCT + BARCODE + DESCRIPTION + WARRANTY
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .Column(product =>
                    {
                        product.Item()
                            .Text(
                                line.ProductName)
                            .SemiBold();

                        // -----------------------------------------
                        // PRIMARY PRODUCT BARCODE
                        // -----------------------------------------

                        if (productBarcodes.TryGetValue(
                                line.ProductVariantId,
                                out var barcode) &&
                            barcode.ImageBytes.Length > 0)
                        {
                            product.Item()
                                .PaddingTop(4)
                                .Width(105)
                                .Height(27)
                                .Image(
                                    barcode.ImageBytes)
                                .FitArea();

                            product.Item()
                                .PaddingTop(1)
                                .Text(
                                    barcode.Value)
                                .FontSize(5.5f)
                                .FontColor(MutedText);
                        }

                        // -----------------------------------------
                        // DESCRIPTION
                        // -----------------------------------------

                        if (!string.IsNullOrWhiteSpace(
                                line.Description))
                        {
                            product.Item()
                                .PaddingTop(2)
                                .Text(
                                    line.Description)
                                .FontSize(6.5f)
                                .FontColor(MutedText);
                        }

                        // -----------------------------------------
                        // WARRANTY
                        // -----------------------------------------

                        if (line.WarrantyMonths.HasValue &&
                            line.WarrantyMonths.Value > 0)
                        {
                            product.Item()
                                .PaddingTop(3)
                                .Text(text =>
                                {
                                    text.Span(
                                            "Warranty / ওয়ারেন্টি: ")
                                        .FontSize(6.5f)
                                        .SemiBold()
                                        .FontColor(accent);

                                    text.Span(
                                            $"{line.WarrantyMonths.Value} " +
                                            $"month" +
                                            $"{(line.WarrantyMonths.Value == 1 ? "" : "s")}")
                                        .FontSize(6.5f);
                                });
                        }
                    });

                // -------------------------------------------------
                // SKU
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .Text(
                        line.SKU);

                // -------------------------------------------------
                // QUANTITY
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        BuildQuantity(
                            line));

                // -------------------------------------------------
                // PRICE
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        FormatMoney(
                            line.UnitPrice));

                // -------------------------------------------------
                // DISCOUNT
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        FormatMoney(
                            line.DiscountAmount));

                // -------------------------------------------------
                // TAX
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        FormatMoney(
                            line.TaxAmount));

                // -------------------------------------------------
                // TOTAL
                // -------------------------------------------------

                BodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        FormatMoney(
                            line.LineTotal))
                    .SemiBold();

                index++;
            }
        });
    }

    private static void HeaderCell(
        IContainer container,
        string text,
        string accent,
        bool alignRight = false)
    {
        var cell =
            container
                .Background(accent)
                .PaddingVertical(8)
                .PaddingHorizontal(5);

        if (alignRight)
        {
            cell.AlignRight()
                .Text(text)
                .FontSize(6.5f)
                .SemiBold()
                .FontColor(
                    Colors.White);

            return;
        }

        cell.Text(text)
            .FontSize(6.5f)
            .SemiBold()
            .FontColor(
                Colors.White);
    }

    private static IContainer BodyCell(
        IContainer container,
        bool alternate)
    {
        return container
            .Background(
                alternate
                    ? SoftBackground
                    : Colors.White)
            .BorderBottom(0.5f)
            .BorderColor(BorderColor)
            .PaddingVertical(7)
            .PaddingHorizontal(5)
            .DefaultTextStyle(
                style =>
                    style.FontSize(7.5f));
    }

    // =========================================================
    // FINANCIAL SUMMARY
    // =========================================================

    private static void ComposeFinancialArea(
        IContainer container,
        SalesQuoteDetailsDto quote,
        string accent)
    {
        // Pricing v2 stores combined discounts and total tax on the quote.
        // Legacy v1 stores header adjustments separately from line values.
        var isPricingV2 = quote.PricingVersion >= 2;
        var lineDiscount = isPricingV2
            ? quote.LineDiscountAmount
            : quote.Lines.Sum(x => x.DiscountAmount);
        var orderDiscount = isPricingV2
            ? quote.OrderDiscountAmount
            : quote.DiscountAmount;
        var totalTax = isPricingV2
            ? quote.TaxAmount
            : quote.TaxAmount + quote.Lines.Sum(x => x.TaxAmount);
        var transport = isPricingV2 ? quote.TransportAmount : 0m;

        container.Column(column =>
        {
            column.Item()
                .AlignRight()
                .Width(280)
                .Column(totals =>
                {
                    totals.Spacing(4);

                    AddTotalRow(
                        totals,
                        "Subtotal / উপমোট",
                        FormatMoney(
                            quote.Subtotal));

                    if (lineDiscount != 0m)
                    {
                        AddTotalRow(
                            totals,
                            "Line discount / পণ্য ছাড় (-)",
                            FormatMoney(lineDiscount));
                    }

                    if (orderDiscount != 0m)
                    {
                        AddTotalRow(
                            totals,
                            "Order discount / অর্ডার ছাড় (-)",
                            FormatMoney(orderDiscount));
                    }

                    AddTotalRow(
                        totals,
                        "VAT / Tax (+)",
                        FormatMoney(totalTax));

                    if (transport != 0m)
                    {
                        AddTotalRow(
                            totals,
                            "Transport / পরিবহন (+)",
                            FormatMoney(transport));
                    }

                    totals.Item()
                        .PaddingTop(3)
                        .BorderTop(0.7f)
                        .BorderColor(BorderColor)
                        .PaddingVertical(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text(
                                    "Total Amount / মোট")
                                .FontSize(9)
                                .SemiBold();

                            row.AutoItem()
                                .Text(
                                    FormatMoney(
                                        quote.TotalAmount))
                                .FontSize(10)
                                .SemiBold();
                        });
                });

            column.Item()
                .PaddingTop(6)
                .Background(accent)
                .PaddingHorizontal(14)
                .PaddingVertical(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(
                            "QUOTED TOTAL / প্রস্তাবিত মোট")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(
                            Colors.White);

                    row.AutoItem()
                        .Text(
                            FormatMoney(
                                quote.TotalAmount))
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(
                            Colors.White);
                });
        });
    }

    private static void AddTotalRow(
        ColumnDescriptor column,
        string label,
        string value)
    {
        column.Item()
            .Row(row =>
            {
                row.RelativeItem()
                    .Text(label)
                    .FontSize(8)
                    .FontColor(MutedText);

                row.AutoItem()
                    .Text(value)
                    .FontSize(8);
            });
    }

    // =========================================================
    // NOTES / TERMS
    // =========================================================

    private static void ComposeTextSection(
        IContainer container,
        string title,
        string text,
        string accent)
    {
        container.Column(column =>
        {
            SectionLabel(
                column,
                title,
                accent);

            column.Item()
                .PaddingTop(5)
                .Text(text)
                .FontSize(8);
        });
    }

    // =========================================================
    // SIGNATURES
    // =========================================================

    private static void ComposeSignatures(
        IContainer container,
        string accent)
    {
        container.Row(row =>
        {
            row.RelativeItem()
                .PaddingRight(50)
                .Column(column =>
                {
                    column.Item()
                        .PaddingTop(20)
                        .LineHorizontal(0.7f)
                        .LineColor(
                            Colors.Grey.Darken1);

                    column.Item()
                        .PaddingTop(4)
                        .AlignCenter()
                        .Text(
                            "Authorized By / অনুমোদিত")
                        .FontSize(8)
                        .SemiBold();
                });

            row.RelativeItem()
                .PaddingLeft(50)
                .Column(column =>
                {
                    column.Item()
                        .PaddingTop(20)
                        .LineHorizontal(0.7f)
                        .LineColor(
                            Colors.Grey.Darken1);

                    column.Item()
                        .PaddingTop(4)
                        .AlignCenter()
                        .Text(
                            "Customer Acceptance / গ্রাহকের সম্মতি")
                        .FontSize(8)
                        .SemiBold();
                });
        });
    }

    // =========================================================
    // FOOTER
    // =========================================================

    private static void ComposeFooter(
        IContainer container,
        BusinessProfileDto? business,
        string accent)
    {
        container.Column(column =>
        {
            column.Item()
                .LineHorizontal(1)
                .LineColor(accent);

            column.Item()
                .PaddingTop(5)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(
                            business is null
                                ? "InventoryPOS"
                                : BuildFooterLine(
                                    business))
                        .FontSize(6.5f)
                        .FontColor(MutedText);

                    row.AutoItem()
                        .DefaultTextStyle(
                            style => style
                                .FontSize(6.5f)
                                .FontColor(MutedText))
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
        });
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static void SectionLabel(
        ColumnDescriptor column,
        string text,
        string accent)
    {
        column.Item()
            .Text(text)
            .FontSize(8)
            .SemiBold()
            .FontColor(accent);
    }

    private static void AddSmallText(
        ColumnDescriptor column,
        string text)
    {
        column.Item()
            .PaddingTop(2)
            .Text(text)
            .FontSize(8)
            .FontColor(MutedText);
    }

    private static string BuildQuantity(
        SalesQuoteDetailsLineDto line)
    {
        var quantity =
            line.Quantity.ToString(
                "0.###");

        return string.IsNullOrWhiteSpace(
                line.UnitSymbol)
            ? quantity
            : $"{quantity} {line.UnitSymbol}";
    }

    private static string FormatMoney(
        decimal amount)
    {
        return $"৳{amount:N2}";
    }

    /*
     * ValidUntilDate is currently stored as timestamptz but
     * represents a business/calendar date. Do not call
     * ToLocalTime here.
     */

    private static string FormatBusinessDate(
        DateTime? date)
    {
        return date.HasValue
            ? date.Value.ToString(
                "dd MMM yyyy")
            : "Not specified";
    }

    private static string GetStatusDisplay(
        SalesQuoteDetailsDto quote)
    {
        return quote.Status switch
        {
            SalesQuoteStatus.Draft =>
                "Draft",

            SalesQuoteStatus.Issued =>
                "Issued",

            SalesQuoteStatus.Accepted =>
                "Accepted",

            SalesQuoteStatus.ConvertedToSale =>
                "Converted to Sale",

            SalesQuoteStatus.Expired =>
                "Expired",

            SalesQuoteStatus.Cancelled =>
                "Cancelled",

            _ =>
                quote.Status.ToString()
        };
    }

    private static string BuildContactLine(
        BusinessProfileDto business)
    {
        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                business.Phone))
        {
            parts.Add(
                business.Phone);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Email))
        {
            parts.Add(
                business.Email);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Website))
        {
            parts.Add(
                business.Website);
        }

        return string.Join(
            "  |  ",
            parts);
    }

    private static string BuildRegistrationLine(
        BusinessProfileDto business)
    {
        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                business.BinNumber))
        {
            parts.Add(
                $"BIN: {business.BinNumber}");
        }

        if (!string.IsNullOrWhiteSpace(
                business.TinNumber))
        {
            parts.Add(
                $"TIN: {business.TinNumber}");
        }

        if (!string.IsNullOrWhiteSpace(
                business.TradeLicenseNumber))
        {
            parts.Add(
                "Trade License: " +
                $"{business.TradeLicenseNumber}");
        }

        return string.Join(
            "  |  ",
            parts);
    }

    private static string BuildFooterLine(
        BusinessProfileDto business)
    {
        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                business.Phone))
        {
            parts.Add(
                business.Phone);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Email))
        {
            parts.Add(
                business.Email);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Website))
        {
            parts.Add(
                business.Website);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Address))
        {
            parts.Add(
                business.Address);
        }

        return parts.Count > 0
            ? string.Join(
                "  •  ",
                parts)
            : business.BusinessName;
    }

    private static string GetAccentColor(
        BusinessProfileDto? business)
    {
        var candidate =
            business?.InvoiceAccentColor;

        if (string.IsNullOrWhiteSpace(
                candidate))
        {
            return DefaultAccentColor;
        }

        candidate =
            candidate.Trim();

        if (!candidate.StartsWith("#"))
        {
            candidate =
                $"#{candidate}";
        }

        if (candidate.Length != 7)
        {
            return DefaultAccentColor;
        }

        for (var i = 1;
             i < candidate.Length;
             i++)
        {
            if (!Uri.IsHexDigit(
                    candidate[i]))
            {
                return DefaultAccentColor;
            }
        }

        return candidate;
    }
}