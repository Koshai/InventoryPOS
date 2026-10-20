using InventoryPOS.Application.DTOs.Reports;

namespace InventoryPOS.Application.Interfaces;

public interface IReportQueryService
{
    Task<SalesSummaryReportDto> GetSalesSummaryAsync(
        ReportFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<InventoryOnHandReportDto> GetInventoryOnHandAsync(
        Guid? locationId,
        int take = 200,
        CancellationToken cancellationToken = default);

    Task<ReceivablesSummaryDto> GetReceivablesSummaryAsync(
        Guid? locationId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);

    Task<DashboardSnapshotDto> GetDashboardSnapshotAsync(
        ReportFilterDto filter,
        CancellationToken cancellationToken = default);
}
