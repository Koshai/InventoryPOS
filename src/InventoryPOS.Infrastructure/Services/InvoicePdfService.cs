using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.DTOs.Settings;
using InventoryPOS.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryPOS.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    private readonly IBusinessProfileService _businessProfileService;
    private readonly IBusinessLogoProvider _businessLogoProvider;

    public InvoicePdfService(
    IBusinessProfileService businessProfileService,
    IBusinessLogoProvider businessLogoProvider)
    {
        _businessProfileService = businessProfileService;
        _businessLogoProvider = businessLogoProvider;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(
    SaleDetailsDto sale,
    CancellationToken cancellationToken = default)
    {
        if (sale is null)
            throw new ArgumentNullException(nameof(sale));

        var businessProfile =
            await _businessProfileService.GetAsync(
                cancellationToken);

        byte[]? logoBytes = null;

        if (!string.IsNullOrWhiteSpace(
                businessProfile?.LogoPath))
        {
            logoBytes =
                await _businessLogoProvider.GetLogoAsync(
                    businessProfile.LogoPath,
                    cancellationToken);
        }

        return Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.Margin(35);

                    page.DefaultTextStyle(
                        x => x
                            .FontSize(9)
                            .FontFamily(
                                "Lato",
                                "Noto Sans Bengali"));

                    page.Header()
                        .Element(header =>
                            ComposeHeader(
                                header,
                                sale,
                                businessProfile,
                                logoBytes));

                    page.Content()
                        .PaddingVertical(15)
                        .Element(content =>
                            ComposeContent(
                                content,
                                sale));

                    page.Footer()
                        .Element(footer =>
                            ComposeFooter(
                                footer,
                                businessProfile));
                });
            })
            .GeneratePdf();
    }

    // =========================================================
    // HEADER
    // =========================================================

    private static void ComposeHeader(
    IContainer container,
    SaleDetailsDto sale,
    BusinessProfileDto? business,
    byte[]? logoBytes)
    {
        container.Row(row =>
        {
            // =====================================================
            // BUSINESS
            // =====================================================

            row.RelativeItem()
    .Row(businessRow =>
    {
        // Logo
        if (logoBytes is { Length: > 0 })
        {
            businessRow
                .ConstantItem(72)
                .Height(72)
                .PaddingRight(12)
                .AlignMiddle()
                .Image(logoBytes)
                .FitArea();
        }

        // Business identity
        businessRow
            .RelativeItem()
            .Column(column =>
            {
                if (business is null)
                {
                    column.Item()
                        .Text("InventoryPOS")
                        .FontSize(20)
                        .SemiBold();

                    column.Item()
                        .PaddingTop(2)
                        .Text(sale.LocationName)
                        .FontSize(9);

                    return;
                }

                column.Item()
                    .Text(business.BusinessName)
                    .FontSize(20)
                    .SemiBold();

                if (!string.IsNullOrWhiteSpace(
                        business.BusinessNameBn))
                {
                    column.Item()
                        .PaddingTop(1)
                        .Text(business.BusinessNameBn)
                        .FontSize(10);
                }

                if (!string.IsNullOrWhiteSpace(
                        business.Address))
                {
                    column.Item()
                        .PaddingTop(4)
                        .Text(business.Address)
                        .FontSize(8);
                }

                if (!string.IsNullOrWhiteSpace(
                        business.AddressBn))
                {
                    column.Item()
                        .PaddingTop(1)
                        .Text(business.AddressBn)
                        .FontSize(8);
                }

                var contact =
                    BuildContactLine(business);

                if (!string.IsNullOrWhiteSpace(contact))
                {
                    column.Item()
                        .PaddingTop(3)
                        .Text(contact)
                        .FontSize(8);
                }

                var registration =
                    BuildRegistrationLine(business);

                if (!string.IsNullOrWhiteSpace(
                        registration))
                {
                    column.Item()
                        .PaddingTop(2)
                        .Text(registration)
                        .FontSize(7)
                        .FontColor(
                            Colors.Grey.Darken1);
                }
            });
    });

            // =====================================================
            // INVOICE
            // =====================================================

            row.ConstantItem(220)
                .AlignRight()
                .Column(column =>
                {
                    column.Item()
                        .AlignRight()
                        .Text("INVOICE")
                        .FontSize(10)
                        .SemiBold()
                        .FontColor(
                            Colors.Grey.Medium);

                    column.Item()
                        .PaddingTop(2)
                        .AlignRight()
                        .Text(sale.InvoiceNumber)
                        .FontSize(16)
                        .SemiBold();

                    column.Item()
                        .PaddingTop(2)
                        .AlignRight()
                        .Text(sale.FormattedSaleDate)
                        .FontSize(8);

                    column.Item()
                        .PaddingTop(5)
                        .AlignRight()
                        .Text(sale.StatusDisplay)
                        .FontSize(8)
                        .SemiBold();

                    column.Item()
                        .PaddingTop(5)
                        .AlignRight()
                        .Text($"Location: {sale.LocationName}")
                        .FontSize(8);
                });
        });
    }

    // =========================================================
    // CONTENT
    // =========================================================

    private static void ComposeContent(
        IContainer container,
        SaleDetailsDto sale)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item()
                .Element(info =>
                    ComposeCustomerInformation(info, sale));

            column.Item()
                .Element(items =>
                    ComposeItemsTable(items, sale));

            column.Item()
                .Element(totals =>
                    ComposeTotals(totals, sale));

            if (sale.HasPayments)
            {
                column.Item()
                    .Element(payments =>
                        ComposePayments(payments, sale));
            }

            column.Item()
                .Element(summary =>
                    ComposePaymentSummary(summary, sale));

            if (!string.IsNullOrWhiteSpace(sale.Notes))
            {
                column.Item()
                    .Element(notes =>
                        ComposeNotes(notes, sale));
            }
        });
    }

    // =========================================================
    // CUSTOMER
    // =========================================================

    private static void ComposeCustomerInformation(
        IContainer container,
        SaleDetailsDto sale)
    {
        container
            .Background(Colors.Grey.Lighten4)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("BILL TO")
                            .FontSize(8)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(3)
                            .Text(sale.CustomerName)
                            .FontSize(11)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(
                                sale.CustomerCode))
                        {
                            column.Item()
                                .PaddingTop(2)
                                .Text(sale.CustomerCode)
                                .FontSize(8);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                sale.CustomerPhone))
                        {
                            column.Item()
                                .PaddingTop(2)
                                .Text(sale.CustomerPhone)
                                .FontSize(8);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                sale.CustomerAddress))
                        {
                            column.Item()
                                .PaddingTop(2)
                                .Text(sale.CustomerAddress)
                                .FontSize(8);
                        }
                    });

                row.RelativeItem()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("LOCATION")
                            .FontSize(8)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(3)
                            .Text(sale.LocationName)
                            .FontSize(11)
                            .SemiBold();
                    });
            });
    }

    // =========================================================
    // ITEMS TABLE
    // =========================================================

    private static void ComposeItemsTable(
        IContainer container,
        SaleDetailsDto sale)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.1f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "PRODUCT");
                HeaderCell(header.Cell(), "QTY");
                HeaderCell(header.Cell(), "PRICE");
                HeaderCell(header.Cell(), "DISCOUNT");
                HeaderCell(header.Cell(), "TAX");
                HeaderCell(header.Cell(), "TOTAL", true);
            });

            foreach (var line in sale.Lines)
            {
                table.Cell()
                    .Element(BodyCell)
                    .Column(column =>
                    {
                        column.Item()
                            .Text(line.ProductName)
                            .SemiBold();

                        column.Item()
                            .PaddingTop(1)
                            .Text(line.SKU)
                            .FontSize(7)
                            .FontColor(Colors.Grey.Medium);
                    });

                table.Cell()
                    .Element(BodyCell)
                    .Text(line.FormattedQuantity);

                table.Cell()
                    .Element(BodyCell)
                    .Text(line.FormattedUnitPrice);

                table.Cell()
                    .Element(BodyCell)
                    .Text(line.FormattedDiscount);

                table.Cell()
                    .Element(BodyCell)
                    .Text(line.FormattedTax);

                table.Cell()
                    .Element(BodyCell)
                    .AlignRight()
                    .Text(line.FormattedLineTotal)
                    .SemiBold();
            }
        });
    }

    private static void HeaderCell(
        IContainer container,
        string text,
        bool alignRight = false)
    {
        var cell =
            container
                .Background(Colors.Grey.Lighten3)
                .PaddingVertical(7)
                .PaddingHorizontal(6);

        if (alignRight)
        {
            cell.AlignRight()
                .Text(text)
                .FontSize(7)
                .SemiBold();

            return;
        }

        cell.Text(text)
            .FontSize(7)
            .SemiBold();
    }

    private static IContainer BodyCell(
        IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(7)
            .PaddingHorizontal(6);
    }

    // =========================================================
    // TOTALS
    // =========================================================

    private static void ComposeTotals(
        IContainer container,
        SaleDetailsDto sale)
    {
        container
            .AlignRight()
            .Width(250)
            .Column(column =>
            {
                column.Spacing(5);

                AddTotalRow(
                    column,
                    "Subtotal",
                    sale.FormattedSubtotal);

                AddTotalRow(
                    column,
                    "Discount",
                    sale.FormattedDiscount);

                AddTotalRow(
                    column,
                    "Tax",
                    sale.FormattedTax);

                column.Item()
                    .PaddingVertical(3)
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten1);

                column.Item()
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text("TOTAL")
                            .FontSize(11)
                            .SemiBold();

                        row.AutoItem()
                            .Text(sale.FormattedTotal)
                            .FontSize(13)
                            .SemiBold();
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
                    .FontColor(Colors.Grey.Darken1);

                row.AutoItem()
                    .Text(value)
                    .FontSize(8);
            });
    }

    // =========================================================
    // PAYMENTS
    // =========================================================

    private static void ComposePayments(
        IContainer container,
        SaleDetailsDto sale)
    {
        container.Column(column =>
        {
            column.Item()
                .Text("PAYMENT DETAILS")
                .FontSize(8)
                .SemiBold()
                .FontColor(Colors.Grey.Darken1);

            column.Item()
                .PaddingTop(5)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.0f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(
                            header.Cell(),
                            "METHOD");

                        HeaderCell(
                            header.Cell(),
                            "REFERENCE");

                        HeaderCell(
                            header.Cell(),
                            "DATE");

                        HeaderCell(
                            header.Cell(),
                            "AMOUNT",
                            true);
                    });

                    foreach (var payment in sale.Payments)
                    {
                        table.Cell()
                            .Element(BodyCell)
                            .Text(payment.PaymentMethodDisplay);

                        table.Cell()
                            .Element(BodyCell)
                            .Text(payment.ReferenceDisplay);

                        table.Cell()
                            .Element(BodyCell)
                            .Text(payment.FormattedPaymentDate);

                        table.Cell()
                            .Element(BodyCell)
                            .AlignRight()
                            .Text(payment.FormattedAmount)
                            .SemiBold();
                    }
                });
        });
    }

    // =========================================================
    // PAID / DUE
    // =========================================================

    private static void ComposePaymentSummary(
        IContainer container,
        SaleDetailsDto sale)
    {
        container
            .Background(Colors.Grey.Lighten4)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("PAID")
                            .FontSize(7)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(2)
                            .Text(sale.FormattedPaid)
                            .FontSize(14)
                            .SemiBold();
                    });

                row.RelativeItem()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("DUE")
                            .FontSize(7)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(2)
                            .Text(sale.FormattedDue)
                            .FontSize(14)
                            .SemiBold();
                    });
            });
    }

    // =========================================================
    // NOTES
    // =========================================================

    private static void ComposeNotes(
        IContainer container,
        SaleDetailsDto sale)
    {
        container.Column(column =>
        {
            column.Item()
                .Text("NOTES")
                .FontSize(8)
                .SemiBold()
                .FontColor(Colors.Grey.Darken1);

            column.Item()
                .PaddingTop(4)
                .Text(sale.Notes ?? string.Empty)
                .FontSize(8);
        });
    }

    // =========================================================
    // FOOTER
    // =========================================================

    private static void ComposeFooter(
    IContainer container)
    {
        container
            .BorderTop(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Thank you for your business.")
                    .FontSize(7)
                    .FontColor(Colors.Grey.Medium);

                row.AutoItem()
                    .AlignRight()
                    .DefaultTextStyle(style =>
                        style
                            .FontSize(7)
                            .FontColor(Colors.Grey.Medium))
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
    }

    private static string BuildContactLine(
    BusinessProfileDto business)
    {
        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                business.Phone))
        {
            parts.Add(business.Phone);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Email))
        {
            parts.Add(business.Email);
        }

        if (!string.IsNullOrWhiteSpace(
                business.Website))
        {
            parts.Add(business.Website);
        }

        return string.Join("  |  ", parts);
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
                $"Trade License: {business.TradeLicenseNumber}");
        }

        return string.Join("  |  ", parts);
    }

    private static void ComposeFooter(
    IContainer container,
    BusinessProfileDto? business)
    {
        var footerText =
            !string.IsNullOrWhiteSpace(
                business?.InvoiceFooter)
                ? business.InvoiceFooter
                : "Thank you for your business.";

        container
            .BorderTop(0.5f)
            .BorderColor(
                Colors.Grey.Lighten2)
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text(footerText)
                    .FontSize(7)
                    .FontColor(
                        Colors.Grey.Medium);

                row.AutoItem()
                    .AlignRight()
                    .DefaultTextStyle(style =>
                        style
                            .FontSize(7)
                            .FontColor(
                                Colors.Grey.Medium))
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
    }
}