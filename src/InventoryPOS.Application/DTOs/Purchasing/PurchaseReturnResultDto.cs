namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseReturnResultDto
{
    public Guid PurchaseReturnId { get; set; }

    public string ReturnNumber { get; set; }
        = string.Empty;

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }
}