using InventoryPOS.Application.DTOs.Settings;
using InventoryPOS.Application.DTOs.WarrantyClaims;
using InventoryPOS.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryPOS.Infrastructure.Services;

public sealed class WarrantyClaimPdfService
    : IWarrantyClaimPdfService
{
    private const string DefaultAccentColor = "#4F56D9";
    private const string SoftBackground = "#F4F5FB";
    private const string BorderColor = "#E1E3EA";
    private const string MutedText = "#747784";

    private readonly IBusinessProfileService
        _businessProfileService;

    private readonly IBusinessLogoProvider
        _businessLogoProvider;

    public WarrantyClaimPdfService(
        IBusinessProfileService businessProfileService,
        IBusinessLogoProvider businessLogoProvider)
    {
        _businessProfileService =
            businessProfileService;

        _businessLogoProvider =
            businessLogoProvider;
    }

    public async Task<byte[]> GenerateWarrantyClaimPdfAsync(
        WarrantyClaimDetailsDto claim,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);

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
                                claim,
                                business,
                                logoBytes,
                                accent));

                    page.Content()
                        .PaddingTop(12)
                        .Element(container =>
                            ComposeContent(
                                container,
                                claim,
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
        WarrantyClaimDetailsDto claim,
        BusinessProfileDto? business,
        byte[]? logoBytes,
        string accent)
    {
        container.Column(column =>
        {
            column.Item()
                .Row(row =>
                {
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

                    row.ConstantItem(170)
                        .AlignRight()
                        .Column(meta =>
                        {
                            meta.Item()
                                .AlignRight()
                                .Text("WARRANTY CLAIM")
                                .FontSize(17)
                                .SemiBold()
                                .FontColor(accent);

                            meta.Item()
                                .PaddingTop(1)
                                .AlignRight()
                                .Text("ওয়ারেন্টি দাবি")
                                .FontSize(10)
                                .SemiBold();

                            meta.Item()
                                .PaddingTop(7)
                                .AlignRight()
                                .Text(claim.ClaimNumber)
                                .FontSize(9)
                                .SemiBold();

                            meta.Item()
                                .PaddingTop(2)
                                .AlignRight()
                                .Text(
                                    $"Status: " +
                                    $"{FormatStatus(claim.Status)}")
                                .FontSize(7.5f)
                                .FontColor(MutedText);
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
                                    "Claim Date / দাবির তারিখ: ")
                                .FontColor(accent)
                                .SemiBold();

                            text.Span(
                                claim.ClaimDate
                                    .ToLocalTime()
                                    .ToString(
                                        "dd MMM yyyy HH:mm"));
                        });

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span(
                                    "Invoice / ইনভয়েস: ")
                                .FontColor(accent)
                                .SemiBold();

                            text.Span(
                                claim.InvoiceNumber);
                        });
                });
        });
    }

    // =========================================================
    // CONTENT
    // =========================================================

    private static void ComposeContent(
        IContainer container,
        WarrantyClaimDetailsDto claim,
        string accent)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item()
                .Element(c =>
                    ComposeCustomerSection(
                        c,
                        claim,
                        accent));

            column.Item()
                .Element(c =>
                    ComposeClaimedProducts(
                        c,
                        claim,
                        accent));

            if (!string.IsNullOrWhiteSpace(
                    claim.CustomerComplaint))
            {
                column.Item()
                    .Element(c =>
                        ComposeTextSection(
                            c,
                            "CUSTOMER COMPLAINT / গ্রাহকের অভিযোগ",
                            claim.CustomerComplaint,
                            accent));
            }

            if (!string.IsNullOrWhiteSpace(
                    claim.InspectionNotes))
            {
                column.Item()
                    .Element(c =>
                        ComposeTextSection(
                            c,
                            "INSPECTION FINDINGS / পরিদর্শন",
                            claim.InspectionNotes,
                            accent));
            }

            if (!string.IsNullOrWhiteSpace(
                    claim.ResolutionNotes))
            {
                column.Item()
                    .Element(c =>
                        ComposeTextSection(
                            c,
                            "RESOLUTION / সমাধান",
                            claim.ResolutionNotes,
                            accent));
            }

            if (claim.Replacements.Count > 0)
            {
                column.Item()
                    .Element(c =>
                        ComposeReplacementHistory(
                            c,
                            claim,
                            accent));
            }

            if (claim.Events.Count > 0)
            {
                column.Item()
                    .Element(c =>
                        ComposeTimeline(
                            c,
                            claim,
                            accent));
            }

            column.Item()
                .PaddingTop(10)
                .Element(c =>
                    ComposeSignatures(
                        c,
                        accent));
        });
    }

    // =========================================================
    // CUSTOMER
    // =========================================================

    private static void ComposeCustomerSection(
        IContainer container,
        WarrantyClaimDetailsDto claim,
        string accent)
    {
        container
            .Background(SoftBackground)
            .Padding(12)
            .Column(column =>
            {
                SectionLabel(
                    column,
                    "CUSTOMER / গ্রাহক",
                    accent);

                column.Item()
                    .PaddingTop(6)
                    .Text(claim.CustomerName)
                    .FontSize(11)
                    .SemiBold();

                if (!string.IsNullOrWhiteSpace(
                        claim.CustomerPhone))
                {
                    column.Item()
                        .PaddingTop(2)
                        .Text(
                            $"Phone: {claim.CustomerPhone}")
                        .FontSize(8)
                        .FontColor(MutedText);
                }

                column.Item()
                    .PaddingTop(2)
                    .Text(
                        $"Original invoice: " +
                        $"{claim.InvoiceNumber}")
                    .FontSize(8)
                    .FontColor(MutedText);
            });
    }

    // =========================================================
    // CLAIMED PRODUCTS
    // =========================================================

    private static void ComposeClaimedProducts(
        IContainer container,
        WarrantyClaimDetailsDto claim,
        string accent)
    {
        container.Column(column =>
        {
            SectionLabel(
                column,
                "CLAIMED PRODUCTS / দাবিকৃত পণ্য",
                accent);

            column.Item()
                .PaddingTop(6)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.0f);
                        columns.RelativeColumn(0.65f);
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(1.15f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(
                            header.Cell(),
                            "SL.",
                            accent);

                        HeaderCell(
                            header.Cell(),
                            "PRODUCT",
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
                            "SERIAL",
                            accent);

                        HeaderCell(
                            header.Cell(),
                            "WARRANTY END",
                            accent);
                    });

                    var index = 1;

                    foreach (var item in claim.Items)
                    {
                        var alternate =
                            index % 2 == 0;

                        BodyCell(
                                table.Cell(),
                                alternate)
                            .Text(
                                index.ToString("00"));

                        BodyCell(
                                table.Cell(),
                                alternate)
                            .Column(product =>
                            {
                                product.Item()
                                    .Text(
                                        item.ProductName)
                                    .SemiBold();

                                if (!string.IsNullOrWhiteSpace(
                                        item.ReportedDefect))
                                {
                                    product.Item()
                                        .PaddingTop(2)
                                        .Text(
                                            item.ReportedDefect)
                                        .FontSize(6.5f)
                                        .FontColor(MutedText);
                                }
                            });

                        BodyCell(
                                table.Cell(),
                                alternate)
                            .Text(
                                item.SKU);

                        BodyCell(
                                table.Cell(),
                                alternate)
                            .AlignRight()
                            .Text(
                                item.Quantity
                                    .ToString("0.###"));

                        BodyCell(
                                table.Cell(),
                                alternate)
                            .Text(
                                string.IsNullOrWhiteSpace(
                                    item.SerialNumber)
                                    ? "—"
                                    : item.SerialNumber);

                        BodyCell(
                                table.Cell(),
                                alternate)
                            .Text(
                                item.OriginalWarrantyEndDate
                                    .HasValue
                                    ? item
                                        .OriginalWarrantyEndDate
                                        .Value
                                        .ToString(
                                            "dd MMM yyyy")
                                    : "—");

                        index++;
                    }
                });
        });
    }

    // =========================================================
    // REPLACEMENTS
    // =========================================================

    private static void ComposeReplacementHistory(
        IContainer container,
        WarrantyClaimDetailsDto claim,
        string accent)
    {
        container.Column(column =>
        {
            SectionLabel(
                column,
                "REPLACEMENT HISTORY / প্রতিস্থাপন",
                accent);

            foreach (var replacement in claim.Replacements)
            {
                column.Item()
                    .PaddingTop(6)
                    .BorderBottom(0.5f)
                    .BorderColor(BorderColor)
                    .PaddingBottom(6)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Column(details =>
                            {
                                details.Item()
                                    .Text(
                                        replacement.ProductName)
                                    .SemiBold();

                                details.Item()
                                    .PaddingTop(2)
                                    .Text(
                                        $"SKU: " +
                                        $"{replacement.SKU}")
                                    .FontSize(7)
                                    .FontColor(MutedText);

                                details.Item()
                                    .PaddingTop(1)
                                    .Text(
                                        replacement.BatchDisplay)
                                    .FontSize(7)
                                    .FontColor(MutedText);
                            });

                        row.RelativeItem()
                            .AlignRight()
                            .Column(details =>
                            {
                                details.Item()
                                    .AlignRight()
                                    .Text(
                                        replacement.QuantityDisplay)
                                    .SemiBold();

                                details.Item()
                                    .PaddingTop(2)
                                    .AlignRight()
                                    .Text(
                                        replacement.IssuedAtDisplay)
                                    .FontSize(7)
                                    .FontColor(MutedText);
                            });
                    });
            }
        });
    }

    // =========================================================
    // TIMELINE
    // =========================================================

    private static void ComposeTimeline(
        IContainer container,
        WarrantyClaimDetailsDto claim,
        string accent)
    {
        container.Column(column =>
        {
            SectionLabel(
                column,
                "CLAIM TIMELINE / দাবির ইতিহাস",
                accent);

            foreach (var entry in claim.Events)
            {
                column.Item()
                    .PaddingTop(6)
                    .BorderLeft(2)
                    .BorderColor(accent)
                    .PaddingLeft(9)
                    .PaddingBottom(5)
                    .Column(eventColumn =>
                    {
                        eventColumn.Item()
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Text(
                                        entry.Description)
                                    .SemiBold();

                                row.AutoItem()
                                    .Text(
                                        entry.CreatedAtDisplay)
                                    .FontSize(6.5f)
                                    .FontColor(MutedText);
                            });

                        if (!string.IsNullOrWhiteSpace(
                                entry.StatusTransitionDisplay))
                        {
                            eventColumn.Item()
                                .PaddingTop(2)
                                .Text(
                                    FormatStatusTransition(
                                        entry
                                            .StatusTransitionDisplay))
                                .FontSize(7)
                                .FontColor(accent);
                        }

                        if (!string.IsNullOrWhiteSpace(
                                entry.Notes))
                        {
                            eventColumn.Item()
                                .PaddingTop(2)
                                .Text(
                                    entry.Notes)
                                .FontSize(7)
                                .FontColor(MutedText);
                        }
                    });
            }
        });
    }

    // =========================================================
    // TEXT SECTIONS
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
                            "Customer / গ্রাহক")
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
    // TABLE HELPERS
    // =========================================================

    private static void HeaderCell(
        IContainer container,
        string text,
        string accent,
        bool alignRight = false)
    {
        var cell =
            container
                .Background(accent)
                .PaddingVertical(7)
                .PaddingHorizontal(4);

        if (alignRight)
        {
            cell
                .AlignRight()
                .Text(text)
                .FontSize(6.5f)
                .SemiBold()
                .FontColor(Colors.White);

            return;
        }

        cell
            .Text(text)
            .FontSize(6.5f)
            .SemiBold()
            .FontColor(Colors.White);
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
            .PaddingHorizontal(4)
            .DefaultTextStyle(
                style =>
                    style.FontSize(7.5f));
    }

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

    // =========================================================
    // DISPLAY HELPERS
    // =========================================================

    private static string FormatStatus(
        string status)
    {
        return status switch
        {
            "UnderInspection" =>
                "Under Inspection",

            "InRepair" =>
                "In Repair",

            "AwaitingReplacement" =>
                "Awaiting Replacement",

            _ => status
        };
    }

    private static string FormatStatusTransition(
        string transition)
    {
        return transition
            .Replace(
                "UnderInspection",
                "Under Inspection")
            .Replace(
                "InRepair",
                "In Repair")
            .Replace(
                "AwaitingReplacement",
                "Awaiting Replacement");
    }

    // =========================================================
    // BUSINESS HELPERS
    // =========================================================

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