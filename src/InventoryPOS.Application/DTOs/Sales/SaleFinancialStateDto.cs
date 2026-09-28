namespace InventoryPOS.Application.DTOs.Sales;

public sealed class SaleFinancialStateDto
{
    public Guid SaleId { get; set; }

    public decimal OriginalTotal { get; set; }

    public decimal ReturnedAmount { get; set; }

    public decimal EffectiveTotal { get; set; }

    public decimal GrossPaidAmount { get; set; }

    public decimal RefundedAmount { get; set; }

    public decimal NetPaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public decimal OverpaymentAmount { get; set; }
}