using InventoryPOS.Application.DTOs.Reports;

namespace InventoryPOS.Application.Reports;

public static class ReportPeriodHelper
{
    public static ReportFilterDto CreateLocalDayRange(
        DateTime localNow,
        TimeSpan localUtcOffset,
        string preset)
    {
        var localDate = DateOnly.FromDateTime(localNow);
        DateOnly fromDay;
        DateOnly toDayExclusive;

        switch (preset.Trim().ToLowerInvariant())
        {
            case "yesterday":
                fromDay = localDate.AddDays(-1);
                toDayExclusive = localDate;
                break;
            case "last7":
            case "last7days":
                fromDay = localDate.AddDays(-6);
                toDayExclusive = localDate.AddDays(1);
                break;
            case "thismonth":
                fromDay = new DateOnly(localDate.Year, localDate.Month, 1);
                toDayExclusive = localDate.AddDays(1);
                break;
            case "today":
            default:
                fromDay = localDate;
                toDayExclusive = localDate.AddDays(1);
                break;
        }

        var offset = localUtcOffset;
        return new ReportFilterDto
        {
            FromUtc = new DateTimeOffset(fromDay.ToDateTime(TimeOnly.MinValue), offset),
            ToUtc = new DateTimeOffset(toDayExclusive.ToDateTime(TimeOnly.MinValue), offset)
        };
    }
}
