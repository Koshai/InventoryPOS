namespace InventoryPOS.Application.DTOs.Reports;

public sealed class ReportFilterDto
{
    public DateTimeOffset FromUtc { get; set; }

    public DateTimeOffset ToUtc { get; set; }

    public Guid? LocationId { get; set; }
}
