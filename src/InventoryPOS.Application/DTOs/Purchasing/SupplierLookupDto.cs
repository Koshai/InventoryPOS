namespace InventoryPOS.Application.DTOs.Purchasing;

public class SupplierLookupDto
{
    public Guid Id { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DisplayName =>
        $"{SupplierCode} - {Name}";
}