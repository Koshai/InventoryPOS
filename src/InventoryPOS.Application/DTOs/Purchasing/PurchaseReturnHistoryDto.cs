using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseReturnHistoryDto
{
    public Guid Id { get; set; }

    public string ReturnNumber { get; set; } = string.Empty;

    public DateTime ReturnDate { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public PurchaseReturnStatus Status { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Reason { get; set; }
    public string FormattedReturnDate =>
    ReturnDate.ToLocalTime().ToString("dd MMM yyyy");

    public string FormattedQuantity =>
        TotalQuantity.ToString("0.###");

    public string FormattedTotalAmount =>
        $"৳{TotalAmount:N2}";
}