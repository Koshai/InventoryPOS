namespace InventoryPOS.Application.DTOs.Products;

public class ProductListItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NameBn { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? BrandName { get; set; }

    public string UnitSymbol { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }

    public bool IsActive { get; set; }

    public string StatusText =>
        IsActive ? "Active" : "Inactive";

    public string FormattedSellingPrice =>
        $"৳{SellingPrice:N2}";
}