namespace InventoryPOS.Application.DTOs.Reports;

public sealed class ReceivablesSummaryDto
{
    public decimal OutstandingTotal { get; set; }

    public int InvoicesWithBalance { get; set; }

    public decimal CurrentBucket { get; set; }

    public decimal Days1To30 { get; set; }

    public decimal Days31To60 { get; set; }

    public decimal Days61To90 { get; set; }

    public decimal Days90Plus { get; set; }
}
