using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InventoryPOS.Application.DTOs.Settings;

namespace InventoryPOS.Infrastructure.Services;

public class PurchaseRequisitionPdfService
    : IPurchaseRequisitionPdfService
{
    private readonly IBusinessProfileService
        _businessProfileService;

    private readonly IBusinessLogoProvider
        _businessLogoProvider;

    private const string DefaultAccentColor =
    "#4F56D9";

    public PurchaseRequisitionPdfService(
        IBusinessProfileService businessProfileService,
        IBusinessLogoProvider businessLogoProvider)
    {
        _businessProfileService =
            businessProfileService;

        _businessLogoProvider =
            businessLogoProvider;
    }

    public async Task<byte[]> GeneratePdfAsync(
        PurchaseRequisitionDetailsDto requisition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requisition);

        var businessProfile =
            await _businessProfileService
                .GetAsync(cancellationToken);

        byte[]? logoBytes = null;

        if (!string.IsNullOrWhiteSpace(
                businessProfile?.LogoPath))
        {
            logoBytes =
                await _businessLogoProvider
                    .GetLogoAsync(
                        businessProfile.LogoPath,
                        cancellationToken);
        }

        var document =
            Document.Create(container =>
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
                                requisition,
                                businessProfile,
                                logoBytes));

                    page.Content()
                        .PaddingVertical(18)
                        .Element(content =>
                            ComposeContent(
                                content,
                                requisition));

                    page.Footer()
                        .Element(ComposeFooter);
                });
            });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(
    IContainer container,
    PurchaseRequisitionDetailsDto requisition,
    BusinessProfileDto? businessProfile,
    byte[]? logoBytes)
    {
        string businessName =
            businessProfile?.BusinessName?.ToString()
            ?? "InventoryPOS";

        string? businessNameBn =
            businessProfile?.BusinessNameBn?.ToString();

        string? address =
            businessProfile?.Address?.ToString();

        string? addressBn =
            businessProfile?.AddressBn?.ToString();

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                if (logoBytes is { Length: > 0 })
                {
                    row.ConstantItem(72)
                        .Height(72)
                        .PaddingRight(12)
                        .AlignMiddle()
                        .Image(logoBytes)
                        .FitArea();
                }

                row.RelativeItem()
                    .Column(company =>
                    {
                        company.Item()
                            .Text(businessName)
                            .FontSize(17)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(
                                businessNameBn))
                        {
                            company.Item()
                                .Text(businessNameBn)
                                .FontSize(10);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                address))
                        {
                            company.Item()
                                .PaddingTop(3)
                                .Text(address)
                                .FontSize(8);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                addressBn))
                        {
                            company.Item()
                                .Text(addressBn)
                                .FontSize(8);
                        }

                        var contact =
                            BuildBusinessContact(
                                businessProfile);

                        if (!string.IsNullOrWhiteSpace(
                                contact))
                        {
                            company.Item()
                                .PaddingTop(2)
                                .Text(contact)
                                .FontSize(8);
                        }
                    });

                row.ConstantItem(185)
                    .AlignRight()
                    .Column(meta =>
                    {
                        meta.Item()
                            .AlignRight()
                            .Text("PURCHASE REQUISITION")
                            .FontSize(15)
                            .Bold();

                        meta.Item()
                            .AlignRight()
                            .Text("REQUEST FOR QUOTATION")
                            .FontSize(10)
                            .SemiBold();

                        meta.Item()
                            .AlignRight()
                            .Text("ক্রয় চাহিদাপত্র / দরপত্র অনুরোধ")
                            .FontSize(8);

                        meta.Item()
                            .PaddingTop(7)
                            .AlignRight()
                            .Text(requisition.RequisitionNumber)
                            .FontSize(10)
                            .SemiBold();

                        meta.Item()
                            .PaddingTop(2)
                            .AlignRight()
                            .Text(
                                requisition.RequisitionDate
                                    .ToLocalTime()
                                    .ToString("dd MMM yyyy"))
                            .FontSize(8);

                        meta.Item()
                            .PaddingTop(2)
                            .AlignRight()
                            .Text(
                                $"Status: {requisition.Status}")
                            .FontSize(8);
                    });
            });

            column.Item()
                .PaddingTop(12)
                .LineHorizontal(1);
        });
    }

    private static void ComposeContent(
        IContainer container,
        PurchaseRequisitionDetailsDto requisition)
    {
        container.Column(column =>
        {
            column.Spacing(16);

            column.Item()
                .Element(x =>
                    ComposeSupplierSection(
                        x,
                        requisition));

            if (!string.IsNullOrWhiteSpace(
                    requisition.Subject))
            {
                column.Item()
                    .Element(x =>
                        ComposeSubject(
                            x,
                            requisition.Subject));
            }

            column.Item()
                .Element(x =>
                    ComposeItemsTable(
                        x,
                        requisition));

            column.Item()
                .Element(x =>
                    ComposeEstimatedTotal(
                        x,
                        requisition));

            if (!string.IsNullOrWhiteSpace(
                    requisition.Notes))
            {
                column.Item()
                    .Element(x =>
                        ComposeTextSection(
                            x,
                            "NOTES",
                            requisition.Notes));
            }

            if (!string.IsNullOrWhiteSpace(
                    requisition.Terms))
            {
                column.Item()
                    .Element(x =>
                        ComposeTextSection(
                            x,
                            "TERMS / REQUEST TO SUPPLIER",
                            requisition.Terms));
            }

            column.Item()
                .PaddingTop(22)
                .Element(ComposeSignatures);
        });
    }

    private static void ComposeSupplierSection(
        IContainer container,
        PurchaseRequisitionDetailsDto requisition)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem()
                    .Column(left =>
                    {
                        left.Item()
                            .Text("SUPPLIER / সরবরাহকারী")
                            .FontSize(7)
                            .SemiBold()
                            .FontColor(
                                Colors.Grey.Darken1);

                        left.Item()
                            .PaddingTop(3)
                            .Text(
                                requisition.SupplierName)
                            .FontSize(11)
                            .SemiBold();
                    });

                row.RelativeItem()
                    .Column(right =>
                    {
                        right.Item()
                            .Text(
                                $"Location: {requisition.LocationName}");

                        right.Item()
                            .PaddingTop(3)
                            .Text(
                                $"Required By: {FormatDate(requisition.RequiredByDate)}");

                        right.Item()
                            .PaddingTop(3)
                            .Text(
                                $"Valid Until: {FormatDate(requisition.ValidUntilDate)}");
                    });
            });
    }

    private static void ComposeSubject(
        IContainer container,
        string subject)
    {
        container.Column(column =>
        {
            column.Item()
                .Text("SUBJECT")
                .FontSize(7)
                .SemiBold()
                .FontColor(
                    Colors.Grey.Darken1);

            column.Item()
                .PaddingTop(3)
                .Text(subject)
                .FontSize(10)
                .SemiBold();
        });
    }

    private static void ComposeItemsTable(
        IContainer container,
        PurchaseRequisitionDetailsDto requisition)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(0.7f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");

                HeaderCell(
                    header.Cell(),
                    "Product / Description");

                HeaderCell(
                    header.Cell(),
                    "SKU");

                HeaderCell(
                    header.Cell(),
                    "Qty",
                    true);

                HeaderCell(
                    header.Cell(),
                    "Est. Unit",
                    true);

                HeaderCell(
                    header.Cell(),
                    "Est. Total",
                    true);
            });

            var number = 1;

            foreach (var line in requisition.Lines)
            {
                BodyCell(
                    table.Cell(),
                    number.ToString());

                table.Cell()
                    .BorderBottom(1)
                    .BorderColor(
                        Colors.Grey.Lighten2)
                    .PaddingVertical(7)
                    .PaddingHorizontal(4)
                    .Column(product =>
                    {
                        product.Item()
                            .Text(line.ProductName)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(
                                line.Description))
                        {
                            product.Item()
                                .PaddingTop(2)
                                .Text(line.Description)
                                .FontSize(7)
                                .FontColor(
                                    Colors.Grey.Darken1);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                line.Notes))
                        {
                            product.Item()
                                .PaddingTop(2)
                                .Text(
                                    $"Note: {line.Notes}")
                                .FontSize(7)
                                .FontColor(
                                    Colors.Grey.Darken1);
                        }
                    });

                BodyCell(
                    table.Cell(),
                    line.SKU);

                BodyCell(
                    table.Cell(),
                    line.RequestedQuantity
                        .ToString("0.###"),
                    true);

                BodyCell(
                    table.Cell(),
                    line.EstimatedUnitCost.HasValue
                        ? $"৳{line.EstimatedUnitCost.Value:N2}"
                        : "Price requested",
                    true);

                BodyCell(
                    table.Cell(),
                    line.EstimatedUnitCost.HasValue
                        ? $"৳{line.EstimatedLineTotal:N2}"
                        : "—",
                    true);

                number++;
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
                .Background(
                    Colors.Grey.Lighten3)
                .PaddingVertical(7)
                .PaddingHorizontal(4);

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

    private static void BodyCell(
        IContainer container,
        string? text,
        bool alignRight = false)
    {
        var cell =
            container
                .BorderBottom(1)
                .BorderColor(
                    Colors.Grey.Lighten2)
                .PaddingVertical(7)
                .PaddingHorizontal(4);

        if (alignRight)
        {
            cell.AlignRight()
                .Text(text ?? string.Empty);

            return;
        }

        cell.Text(text ?? string.Empty);
    }

    private static void ComposeEstimatedTotal(
        IContainer container,
        PurchaseRequisitionDetailsDto requisition)
    {
        container.Row(row =>
        {
            row.RelativeItem();

            row.ConstantItem(235)
                .Column(column =>
                {
                    column.Item()
                        .Row(total =>
                        {
                            total.RelativeItem()
                                .Text("Estimated Total")
                                .SemiBold();

                            total.ConstantItem(100)
                                .AlignRight()
                                .Text(
                                    $"৳{requisition.EstimatedTotal:N2}")
                                .FontSize(12)
                                .Bold();
                        });

                    if (requisition.Lines.Any(
                            x => !x.EstimatedUnitCost.HasValue))
                    {
                        column.Item()
                            .PaddingTop(5)
                            .AlignRight()
                            .Text(
                                "Unpriced items are excluded from the estimated total.")
                            .FontSize(7)
                            .FontColor(
                                Colors.Grey.Darken1);
                    }
                });
        });
    }

    private static void ComposeTextSection(
        IContainer container,
        string title,
        string text)
    {
        container.Column(column =>
        {
            column.Item()
                .Text(title)
                .FontSize(7)
                .SemiBold()
                .FontColor(
                    Colors.Grey.Darken1);

            column.Item()
                .PaddingTop(4)
                .Text(text)
                .FontSize(9);
        });
    }

    private static void ComposeSignatures(
        IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem()
                .PaddingRight(30)
                .Column(column =>
                {
                    column.Item()
                        .PaddingTop(25)
                        .LineHorizontal(1);

                    column.Item()
                        .PaddingTop(4)
                        .Text("Prepared / Authorized By")
                        .FontSize(7);
                });

            row.RelativeItem()
                .PaddingLeft(30)
                .Column(column =>
                {
                    column.Item()
                        .PaddingTop(25)
                        .LineHorizontal(1);

                    column.Item()
                        .PaddingTop(4)
                        .Text("Supplier / Authorized Representative")
                        .FontSize(7);
                });
        });
    }

    private static void ComposeFooter(
        IContainer container)
    {
        container
            .BorderTop(1)
            .BorderColor(
                Colors.Grey.Lighten2)
            .PaddingTop(7)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text(
                        "Purchase Requisition / Request for Quotation")
                    .FontSize(7)
                    .FontColor(
                        Colors.Grey.Darken1);

                row.RelativeItem()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.Span("Page ")
                            .FontSize(7);

                        text.CurrentPageNumber()
                            .FontSize(7);

                        text.Span(" of ")
                            .FontSize(7);

                        text.TotalPages()
                            .FontSize(7);
                    });
            });
    }

    private static string FormatDate(
        DateTime? date)
    {
        return date.HasValue
            ? date.Value
                .ToLocalTime()
                .ToString("dd MMM yyyy")
            : "Not specified";
    }

    private static string BuildBusinessContact(
    BusinessProfileDto? profile)
    {
        if (profile is null)
            return string.Empty;

        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                profile.Phone))
        {
            parts.Add(profile.Phone);
        }

        if (!string.IsNullOrWhiteSpace(
                profile.Email))
        {
            parts.Add(profile.Email);
        }

        if (!string.IsNullOrWhiteSpace(
                profile.Website))
        {
            parts.Add(profile.Website);
        }

        return string.Join(
            "  |  ",
            parts);
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