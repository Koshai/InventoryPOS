namespace InventoryPOS.Application.DTOs.Purchasing;

public class GoodsReceiptListItemDto
{
    public Guid Id { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; }

    public int LineCount { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalValue { get; set; }

    public string FormattedReceiptDate =>
        ReceiptDate.ToLocalTime().ToString("MMM dd, yyyy");

    public string FormattedTotalValue =>
        $"৳{TotalValue:N2}";
}