namespace InventoryPOS.Application.DTOs.Products;

public class ProductDetailsDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NameBn { get; set; }

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public Guid? BrandId { get; set; }

    public string? BrandName { get; set; }

    public Guid UnitId { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<ProductVariantDto> Variants { get; set; } = [];

    public Guid? VariantId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool VariantIsActive { get; set; } = true;
}

public class ProductVariantDto
{
    public Guid Id { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? NameBn { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; }
}