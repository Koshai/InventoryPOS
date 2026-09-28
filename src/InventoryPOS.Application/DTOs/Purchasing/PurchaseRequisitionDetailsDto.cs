using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseRequisitionDetailsDto
{
    public Guid Id { get; set; }

    public string RequisitionNumber { get; set; } = string.Empty;

    public DateTime RequisitionDate { get; set; }

    public DateTime? RequiredByDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    public PurchaseRequisitionStatus Status { get; set; }

    public Guid SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public string? Notes { get; set; }

    public string? Terms { get; set; }

    public decimal EstimatedTotal { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public string? PurchaseOrderNumber { get; set; }

    public List<PurchaseRequisitionDetailsLineDto> Lines { get; set; } = [];
}

public class PurchaseRequisitionDetailsLineDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public decimal RequestedQuantity { get; set; }

    public decimal? EstimatedUnitCost { get; set; }

    public decimal EstimatedLineTotal { get; set; }

    public string? Description { get; set; }

    public string? Notes { get; set; }
    public string RequestedQuantityDisplay =>
    RequestedQuantity.ToString("0.###");

    public string EstimatedUnitCostDisplay =>
        EstimatedUnitCost.HasValue
            ? $"৳{EstimatedUnitCost.Value:N2}"
            : "Price requested";

    public string EstimatedLineTotalDisplay =>
        EstimatedUnitCost.HasValue
            ? $"৳{EstimatedLineTotal:N2}"
            : "—";
}