namespace InventoryPOS.Application.DTOs.SalesReturns;

public sealed class CreateSalesReturnResultDto
{
    public Guid SalesReturnId { get; set; }

    public string ReturnNumber { get; set; } = string.Empty;

    public decimal ReturnAmount { get; set; }

    public decimal CreditAdjustmentAmount { get; set; }

    /*
     * Maximum money that became refundable because of this return.
     */
    public decimal RefundableAmount { get; set; }

    /*
     * Money actually refunded during this transaction.
     */
    public decimal RefundedAmount { get; set; }

    /*
     * Refundable amount still not physically paid out.
     */
    public decimal RemainingRefundableAmount { get; set; }

    public decimal RemainingSaleDue { get; set; }

    public List<CreateSalesReturnLineResultDto> Lines { get; set; }
        = new();
}

public sealed class CreateSalesReturnLineResultDto
{
    public Guid SaleLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsRestockable { get; set; }
}