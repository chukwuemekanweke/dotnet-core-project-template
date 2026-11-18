using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Domain.UnitTests.Common.Analytics;

public sealed class When_CalculatingAnalyticsCalendarPeriods_Should
{
    [Theory]
    [InlineData(Interval.Day, "2026-11-18", "2026-11-18", "2026-11-19")]
    [InlineData(Interval.Week, "2026-11-18", "2026-11-16", "2026-11-23")]
    [InlineData(Interval.Month, "2026-11-18", "2026-11-01", "2026-12-01")]
    [InlineData(Interval.Year, "2026-11-18", "2026-01-01", "2027-01-01")]
    public void AlignAndAdvanceCalendarBoundaries(
        Interval interval,
        string dateValue,
        string expectedStartValue,
        string expectedNextValue)
    {
        var date = DateOnly.Parse(dateValue);
        var expectedStart = DateOnly.Parse(expectedStartValue);
        var expectedNext = DateOnly.Parse(expectedNextValue);

        var periodStart = AnalyticsCalendar.AlignPeriodStart(date, interval);

        periodStart.ShouldBe(expectedStart);
        AnalyticsCalendar.AdvancePeriod(periodStart, interval).ShouldBe(expectedNext);
    }

    [Fact]
    public void CapPeriodEndAtReportingRangeEnd()
    {
        var periodStart = new DateOnly(2026, 11, 1);

        var periodEnd = AnalyticsCalendar.GetPeriodEnd(
            periodStart,
            new DateOnly(2026, 11, 18),
            Interval.Month);

        periodEnd.ShouldBe(new DateOnly(2026, 11, 18));
    }
}
