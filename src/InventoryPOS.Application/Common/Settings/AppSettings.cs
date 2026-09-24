using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryPOS.Application.Common.Settings;

public sealed class AppSettings
{
    public const string SectionName = "App";

    public string Name { get; set; } = "InventoryPOS";

    public string DefaultLanguage { get; set; } = "en-US";

    public string DefaultCurrency { get; set; } = "BDT";
}
