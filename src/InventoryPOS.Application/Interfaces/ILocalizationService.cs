using System;
using System.Collections.Generic;
using System.Text;
using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.Interfaces;

public interface ILocalizationService
{
    SupportedLanguage CurrentLanguage { get; }

    string GetString(string key);

    Task ChangeLanguageAsync(SupportedLanguage language);
}
