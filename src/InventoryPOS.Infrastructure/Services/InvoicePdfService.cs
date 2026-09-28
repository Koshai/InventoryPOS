using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.DTOs.Settings;
using InventoryPOS.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    private const string DefaultAccentColor = "#4F56D9";
    private const string SoftBackground = "#F4F5FB";
    private const string BorderColor = "#E1E3EA";
    private const string MutedText = "#747784";

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
                                sale,
                                business,
                                logoBytes,
                                accent));

                    page.Content()
                        .PaddingTop(12)
                        .Element(container =>
                            ComposeContent(
                                container,
                                sale,
                                business,
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
        SaleDetailsDto sale,
        BusinessProfileDto? business,
        byte[]? logoBytes,
        string accent)
    {
        container.Column(column =>
        {
            column.Item()
                .Row(row =>
                {
                    // -----------------------------------------
                    // LOGO
                    // -----------------------------------------

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

                    // -----------------------------------------
                    // BUSINESS IDENTITY
                    // -----------------------------------------

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

                    // -----------------------------------------
                    // RESERVED QR AREA
                    // -----------------------------------------

                    row.ConstantItem(72)
                        .AlignRight()
                        .Column(qr =>
                        {
                            qr.Item()
                                .AlignRight()
                                .Width(52)
                                .Height(52)
                                .Border(1)
                                .BorderColor(accent)
                                .AlignCenter()
                                .AlignMiddle()
                                .Text("QR")
                                .FontSize(9)
                                .SemiBold()
                                .FontColor(accent);

                            qr.Item()
                                .PaddingTop(2)
                                .AlignRight()
                                .Text("Invoice verification")
                                .FontSize(5.5f)
                                .FontColor(MutedText);
                        });
                });

            column.Item()
                .PaddingTop(10)
                .LineHorizontal(1)
                .LineColor(accent);

            column.Item()
                .PaddingTop(10)
                .AlignCenter()
                .Text("SALES INVOICE / বিক্রয় চালান")
                .FontSize(15)
                .SemiBold();

            column.Item()
                .PaddingTop(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(text =>
                        {
                            text.Span("Date / তারিখ: ")
                                .FontColor(accent)
                                .SemiBold();

                            text.Span(
                                sale.SaleDate
                                    .ToString(
                                        "dd MMM yyyy"));
                        });

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span(
                                    "Invoice No. / চালান নং: ")
                                .FontColor(accent)
                                .SemiBold();

                            text.Span(
                                    sale.InvoiceNumber)
                                .SemiBold();
                        });
                });
        });
    }

    // =========================================================
    // CONTENT
    // =========================================================

    private static void ComposeContent(
        IContainer container,
        SaleDetailsDto sale,
        BusinessProfileDto? business,
        string accent)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item()
                .Element(c =>
                    ComposeParties(
                        c,
                        sale,
                        business,
                        accent));

            column.Item()
                .Element(c =>
                    ComposeItemsTable(
                        c,
                        sale,
                        accent));

            column.Item()
                .Element(c =>
                    ComposeFinancialArea(
                        c,
                        sale,
                        accent));

            if (sale.HasPayments)
            {
                column.Item()
                    .Element(c =>
                        ComposePayments(
                            c,
                            sale,
                            accent));
            }

            column.Item()
                .Element(c =>
                    ComposeNotesAndTerms(
                        c,
                        sale,
                        business,
                        accent));

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
        SaleDetailsDto sale,
        BusinessProfileDto? business,
        string accent)
    {
        container
            .PaddingVertical(4)
            .Row(row =>
            {
                // BILL TO

                row.RelativeItem()
                    .PaddingRight(20)
                    .Column(column =>
                    {
                        SectionLabel(
                            column,
                            "BILL TO / ক্রেতা",
                            accent);

                        column.Item()
                            .PaddingTop(5)
                            .Text(
                                sale.CustomerName)
                            .FontSize(11)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(
                                sale.CustomerCode))
                        {
                            AddSmallText(
                                column,
                                $"Customer Code: {sale.CustomerCode}");
                        }

                        if (!string.IsNullOrWhiteSpace(
                                sale.CustomerPhone))
                        {
                            AddSmallText(
                                column,
                                $"Phone: {sale.CustomerPhone}");
                        }

                        if (!string.IsNullOrWhiteSpace(
                                sale.CustomerAddress))
                        {
                            AddSmallText(
                                column,
                                sale.CustomerAddress);
                        }
                    });

                // SOLD BY

                row.RelativeItem()
                    .AlignRight()
                    .Column(column =>
                    {
                        column.Item()
                            .AlignRight()
                            .Text("SOLD BY / বিক্রেতা")
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
                                sale.LocationName)
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
                                    .Text(registration)
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
    SaleDetailsDto sale,
    string accent)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);      // SL
                columns.RelativeColumn(2.5f);   // Product
                columns.RelativeColumn(1.0f);   // SKU
                columns.RelativeColumn(0.65f);  // Qty
                columns.RelativeColumn(1.0f);   // Unit price
                columns.RelativeColumn(0.8f);   // Discount
                columns.RelativeColumn(0.7f);   // Tax
                columns.RelativeColumn(1.05f);  // Total
            });

            table.Header(header =>
            {
                InvoiceHeaderCell(
                    header.Cell(),
                    "SL.",
                    accent);

                InvoiceHeaderCell(
                    header.Cell(),
                    "PRODUCT / পণ্য",
                    accent);

                InvoiceHeaderCell(
                    header.Cell(),
                    "SKU",
                    accent);

                InvoiceHeaderCell(
                    header.Cell(),
                    "QTY",
                    accent,
                    true);

                InvoiceHeaderCell(
                    header.Cell(),
                    "PRICE",
                    accent,
                    true);

                InvoiceHeaderCell(
                    header.Cell(),
                    "DISC.",
                    accent,
                    true);

                InvoiceHeaderCell(
                    header.Cell(),
                    "TAX",
                    accent,
                    true);

                InvoiceHeaderCell(
                    header.Cell(),
                    "TOTAL",
                    accent,
                    true);
            });

            var index = 1;

            foreach (var line in sale.Lines)
            {
                var alternate =
                    index % 2 == 0;

                // ---------------------------------------------
                // SL
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .Text(
                        index.ToString("00"));

                // ---------------------------------------------
                // PRODUCT + WARRANTY
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .Column(column =>
                    {
                        column.Item()
                            .Text(line.ProductName)
                            .SemiBold();

                        if (line.HasWarranty)
                        {
                            column.Item()
                                .PaddingTop(3)
                                .Text(text =>
                                {
                                    text.Span(
                                            "Warranty / ওয়ারেন্টি: ")
                                        .FontSize(6.5f)
                                        .SemiBold()
                                        .FontColor(accent);

                                    text.Span(
                                            $"{line.WarrantyMonths!.Value} " +
                                            $"month" +
                                            $"{(line.WarrantyMonths.Value == 1 ? "" : "s")}")
                                        .FontSize(6.5f);
                                });

                            if (line.WarrantyEndDate.HasValue)
                            {
                                column.Item()
                                    .PaddingTop(1)
                                    .Text(
                                        $"Valid until / বৈধ: " +
                                        $"{line.WarrantyEndDate.Value:dd MMM yyyy}")
                                    .FontSize(6.25f)
                                    .FontColor(MutedText);
                            }
                        }
                    });

                // ---------------------------------------------
                // SKU
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .Text(line.SKU);

                // ---------------------------------------------
                // QUANTITY
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        line.FormattedQuantity);

                // ---------------------------------------------
                // UNIT PRICE
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        line.FormattedUnitPrice);

                // ---------------------------------------------
                // DISCOUNT
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        line.FormattedDiscount);

                // ---------------------------------------------
                // TAX
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        line.FormattedTax);

                // ---------------------------------------------
                // TOTAL
                // ---------------------------------------------

                InvoiceBodyCell(
                        table.Cell(),
                        alternate)
                    .AlignRight()
                    .Text(
                        line.FormattedLineTotal)
                    .SemiBold();

                index++;
            }
        });
    }

    private static void InvoiceHeaderCell(
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
                .FontColor(Colors.White);

            return;
        }

        cell.Text(text)
            .FontSize(6.5f)
            .SemiBold()
            .FontColor(Colors.White);
    }

    private static IContainer InvoiceBodyCell(
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
        SaleDetailsDto sale,
        string accent)
    {
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
                        sale.FormattedSubtotal);

                    AddTotalRow(
                        totals,
                        "Discount / ছাড়",
                        sale.FormattedDiscount);

                    AddTotalRow(
                        totals,
                        "VAT / Tax",
                        sale.FormattedTax);

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
                                    sale.FormattedTotal)
                                .FontSize(10)
                                .SemiBold();
                        });
                });

            // GRAND TOTAL BAR

            column.Item()
                .PaddingTop(6)
                .Background(accent)
                .PaddingHorizontal(14)
                .PaddingVertical(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(
                            "GRAND TOTAL / সর্বমোট")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(Colors.White);

                    row.AutoItem()
                        .Text(
                            sale.FormattedTotal)
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(Colors.White);
                });

            // PAID / DUE

            column.Item()
                .Background(SoftBackground)
                .PaddingHorizontal(14)
                .PaddingVertical(9)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Column(paid =>
                        {
                            paid.Item()
                                .Text(
                                    "PAID / পরিশোধ")
                                .FontSize(7)
                                .FontColor(MutedText);

                            paid.Item()
                                .PaddingTop(2)
                                .Text(
                                    sale.FormattedPaid)
                                .FontSize(11)
                                .SemiBold();
                        });

                    row.RelativeItem()
                        .AlignRight()
                        .Column(due =>
                        {
                            due.Item()
                                .AlignRight()
                                .Text(
                                    "DUE / বাকি")
                                .FontSize(7)
                                .FontColor(MutedText);

                            due.Item()
                                .PaddingTop(2)
                                .AlignRight()
                                .Text(
                                    sale.FormattedDue)
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(
                                    sale.HasDue
                                        ? accent
                                        : Colors.Black);
                        });
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
    // PAYMENT DETAILS
    // =========================================================

    private static void ComposePayments(
        IContainer container,
        SaleDetailsDto sale,
        string accent)
    {
        container.Column(column =>
        {
            SectionLabel(
                column,
                "PAYMENT DETAILS / পেমেন্ট বিবরণ",
                accent);

            column.Item()
                .PaddingTop(5)
                .Table(table =>
                {
                    table.ColumnsDefinition(
                        columns =>
                        {
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.0f);
                        });

                    table.Header(header =>
                    {
                        SmallHeaderCell(
                            header.Cell(),
                            "METHOD");

                        SmallHeaderCell(
                            header.Cell(),
                            "REFERENCE");

                        SmallHeaderCell(
                            header.Cell(),
                            "DATE");

                        SmallHeaderCell(
                            header.Cell(),
                            "AMOUNT",
                            true);
                    });

                    foreach (
                        var payment
                        in sale.Payments)
                    {
                        SmallBodyCell(
                                table.Cell())
                            .Text(
                                payment.PaymentMethodDisplay);

                        SmallBodyCell(
                                table.Cell())
                            .Text(
                                payment.ReferenceDisplay);

                        SmallBodyCell(
                                table.Cell())
                            .Text(
                                payment.FormattedPaymentDate);

                        SmallBodyCell(
                                table.Cell())
                            .AlignRight()
                            .Text(
                                payment.FormattedAmount)
                            .SemiBold();
                    }
                });
        });
    }

    private static void SmallHeaderCell(
        IContainer container,
        string text,
        bool alignRight = false)
    {
        var cell =
            container
                .Background(SoftBackground)
                .PaddingVertical(6)
                .PaddingHorizontal(6);

        if (alignRight)
        {
            cell.AlignRight()
                .Text(text)
                .FontSize(6.5f)
                .SemiBold();

            return;
        }

        cell.Text(text)
            .FontSize(6.5f)
            .SemiBold();
    }

    private static IContainer SmallBodyCell(
        IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(BorderColor)
            .PaddingVertical(6)
            .PaddingHorizontal(6)
            .DefaultTextStyle(
                style =>
                    style.FontSize(7.5f));
    }

    // =========================================================
    // NOTES / TERMS
    // =========================================================

    private static void ComposeNotesAndTerms(
        IContainer container,
        SaleDetailsDto sale,
        BusinessProfileDto? business,
        string accent)
    {
        container.Column(column =>
        {
            SectionLabel(
                column,
                "NOTES / TERMS",
                accent);

            if (!string.IsNullOrWhiteSpace(
                    sale.Notes))
            {
                column.Item()
                    .PaddingTop(5)
                    .Text(sale.Notes)
                    .FontSize(8);
            }

            if (!string.IsNullOrWhiteSpace(
                    business?.InvoiceFooter))
            {
                column.Item()
                    .PaddingTop(4)
                    .Text(
                        business.InvoiceFooter)
                    .FontSize(8);
            }

            if (string.IsNullOrWhiteSpace(
                    sale.Notes) &&
                string.IsNullOrWhiteSpace(
                    business?.InvoiceFooter))
            {
                column.Item()
                    .PaddingTop(5)
                    .Text(
                        "Thank you for your purchase. / ক্রয়ের জন্য ধন্যবাদ।")
                    .FontSize(8);
            }
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
                            "Received By / গ্রহণকারী")
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
                $"Trade License: {business.TradeLicenseNumber}");
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

        if (!string.IsNullOrWhiteSpace(
                business.Address))
        {
            parts.Add(business.Address);
        }

        return parts.Count > 0
            ? string.Join("  •  ", parts)
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