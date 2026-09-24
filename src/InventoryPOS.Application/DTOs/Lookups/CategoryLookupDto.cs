using System;
namespace InventoryPOS.Application.DTOs.Lookups;

public class CategoryLookupDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}