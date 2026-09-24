namespace InventoryPOS.Application.DTOs.Purchasing;

public class GoodsReceiptDetailsDto
{
    public Guid Id { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; }

    public string PurchaseOrderNumber { get; set; } = string.Empty;

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public int LineCount => Lines.Count;

    public IReadOnlyList<GoodsReceiptLineDetailsDto> Lines { get; set; }
        = Array.Empty<GoodsReceiptLineDetailsDto>();

    public decimal TotalQuantity =>
        Lines.Sum(x => x.ReceivedQuantity);

    public decimal TotalValue =>
        Lines.Sum(x => x.LineTotal);

    public string FormattedReceiptDate =>
        ReceiptDate.ToLocalTime().ToString("MMM dd, yyyy");

    public string FormattedTotalValue =>
        $"৳{TotalValue:N2}";

    public string NotesDisplay =>
        string.IsNullOrWhiteSpace(Notes)
            ? "No notes"
            : Notes;
}