namespace InventoryPOS.Application.DTOs.Products;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;

    public string? NameBn { get; set; }

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public Guid UnitId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;
}