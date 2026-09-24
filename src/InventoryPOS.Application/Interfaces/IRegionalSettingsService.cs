using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace InventoryPOS.Application.Interfaces;

public interface IRegionalSettingsService
{
    CultureInfo CurrentCulture { get; }

    string CurrencyCode { get; }

    string CurrencySymbol { get; }

    string FormatCurrency(decimal amount);

    string FormatDate(DateTime date);
}
