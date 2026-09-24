namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseOrderDetailsDto
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string SupplierCode { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public IReadOnlyList<PurchaseOrderLineDto> Lines { get; set; }
        = Array.Empty<PurchaseOrderLineDto>();

    public decimal TotalAmount =>
        Lines.Sum(x => x.LineTotal);

    public string FormattedTotal =>
        $"৳{TotalAmount:N2}";

    public string FormattedOrderDate =>
    OrderDate.ToLocalTime()
        .ToString("MMM dd, yyyy");

    public string FormattedExpectedDate =>
        ExpectedDate.HasValue
            ? ExpectedDate.Value.ToLocalTime()
                .ToString("MMM dd, yyyy")
            : "—";

    public string NotesDisplay =>
        string.IsNullOrWhiteSpace(Notes)
            ? "No notes"
            : Notes;
}