using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseRequisitionHistoryDto
{
    public Guid Id { get; set; }

    public string RequisitionNumber { get; set; } = string.Empty;

    public DateTime RequisitionDate { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public PurchaseRequisitionStatus Status { get; set; }

    public int LineCount { get; set; }

    public decimal TotalRequestedQuantity { get; set; }

    public decimal EstimatedTotal { get; set; }

    public DateTime? RequiredByDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public string? Subject { get; set; }

    public string RequisitionDateDisplay =>
    RequisitionDate.ToLocalTime()
        .ToString("dd MMM yyyy");

    public string TotalRequestedQuantityDisplay =>
        TotalRequestedQuantity.ToString("0.###");

    public string EstimatedTotalDisplay =>
        $"৳{EstimatedTotal:N2}";
}