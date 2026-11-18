namespace BackendProjectTemplate.Domain.Common.Analytics;

public static class AnalyticsCalendar
{
    public static DateOnly AlignPeriodStart(DateOnly date, Interval interval) => interval switch
    {
        Interval.Day => date,
        Interval.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        Interval.Month => new DateOnly(date.Year, date.Month, 1),
        Interval.Year => new DateOnly(date.Year, 1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };

    public static DateOnly AdvancePeriod(DateOnly date, Interval interval) => interval switch
    {
        Interval.Day => date.AddDays(1),
        Interval.Week => date.AddDays(7),
        Interval.Month => date.AddMonths(1),
        Interval.Year => date.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };

    public static DateOnly GetPeriodEnd(DateOnly periodStart, DateOnly rangeEnd, Interval interval)
    {
        var calendarPeriodEnd = AdvancePeriod(periodStart, interval).AddDays(-1);
        return calendarPeriodEnd <= rangeEnd ? calendarPeriodEnd : rangeEnd;
    }
}
