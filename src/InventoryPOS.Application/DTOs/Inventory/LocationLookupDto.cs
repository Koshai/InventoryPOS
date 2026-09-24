namespace InventoryPOS.Application.DTOs.Inventory;

public class LocationLookupDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string DisplayName =>
        $"{Name} ({Code})";
}