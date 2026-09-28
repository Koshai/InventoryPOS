namespace InventoryPOS.Application.DTOs.Purchasing;

public class PurchaseRequisitionResultDto
{
    public Guid PurchaseRequisitionId { get; set; }

    public string RequisitionNumber { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public decimal TotalRequestedQuantity { get; set; }

    public decimal EstimatedTotal { get; set; }
}