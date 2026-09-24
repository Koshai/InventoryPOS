namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseOrderListItemDto
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public decimal TotalAmount { get; set; }

    public string FormattedOrderDate =>
        OrderDate.ToLocalTime().ToString("MMM dd, yyyy");

    public string FormattedExpectedDate =>
        ExpectedDate.HasValue
            ? ExpectedDate.Value.ToLocalTime().ToString("MMM dd, yyyy")
            : "—";

    public string FormattedTotal =>
        $"৳{TotalAmount:N2}";
}