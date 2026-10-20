using InventoryPOS.Application.Reports;

namespace InventoryPOS.Tests.Reports;

public sealed class ReportPeriodHelperTests
{
    [Fact]
    public void Today_UsesLocalDayBoundaries()
    {
        var localNow = new DateTime(2026, 10, 17, 15, 30, 0);
        var offset = TimeSpan.FromHours(6);

        var filter = ReportPeriodHelper.CreateLocalDayRange(localNow, offset, "today");

        Assert.Equal(new DateTimeOffset(2026, 10, 17, 0, 0, 0, offset), filter.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 10, 18, 0, 0, 0, offset), filter.ToUtc);
    }

    [Fact]
    public void Last7Days_IncludesTodayAndSixPriorDays()
    {
        var localNow = new DateTime(2026, 10, 17, 8, 0, 0);
        var offset = TimeSpan.Zero;

        var filter = ReportPeriodHelper.CreateLocalDayRange(localNow, offset, "last7");

        Assert.Equal(new DateTimeOffset(2026, 10, 11, 0, 0, 0, offset), filter.FromUtc);
        Assert.Equal(new DateTimeOffset(2026, 10, 18, 0, 0, 0, offset), filter.ToUtc);
    }
}
