namespace InventoryPOS.Application.DTOs.Lookups;

public class UnitLookupDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;
}