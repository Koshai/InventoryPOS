using InventoryPOS.Application.Interfaces;
using System;
using System.Globalization;

namespace InventoryPOS.Desktop.Services;

public sealed class RegionalSettingsService
    : IRegionalSettingsService
{
    public CultureInfo CurrentCulture { get; private set; }
        = new("en-US");

    public string CurrencyCode => "BDT";

    public string CurrencySymbol => "৳";

    public string FormatCurrency(decimal amount)
    {
        return $"{CurrencySymbol}{amount:N2}";
    }

    public string FormatDate(DateTime date)
    {
        return date.ToString(
            "dd-MM-yyyy",
            CurrentCulture);
    }
}