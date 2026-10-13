namespace InventoryPOS.Application.DTOs.Products;

public sealed class ProductBarcodeLabelRequest
{
    public Guid ProductVariantId { get; set; }

    public Guid ProductBarcodeId { get; set; }

    public int Copies { get; set; } = 1;

    public bool ShowPrice { get; set; } = true;

    public bool ShowSku { get; set; } = true;
}