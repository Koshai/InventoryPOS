namespace InventoryPOS.Application.DTOs.Settings;

public class BusinessProfileDto
{
    public Guid? Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessNameBn { get; set; }

    public string? Address { get; set; }
    public string? AddressBn { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? BinNumber { get; set; }
    public string? TinNumber { get; set; }
    public string? TradeLicenseNumber { get; set; }

    public string CurrencyCode { get; set; } = "BDT";
    public string CurrencySymbol { get; set; } = "৳";

    public string? LogoPath { get; set; }
    public string InvoiceAccentColor { get; set; } = "#4F56D9";

    public string? InvoiceFooter { get; set; }
}