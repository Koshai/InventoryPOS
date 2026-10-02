
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class IssueWarrantyReplacementRequest
{
    public Guid ClaimId { get; set; }

    public Guid ClaimItemId { get; set; }

    public decimal Quantity { get; set; }

    public string? Notes { get; set; }
}

public sealed class IssueWarrantyReplacementResult
{
    public Guid ClaimId { get; set; }

    public Guid ClaimItemId { get; set; }

    public decimal QuantityIssued { get; set; }

    public decimal RemainingQuantity { get; set; }

    public bool ClaimResolved { get; set; }

    public int StockMovementsCreated { get; set; }
}
