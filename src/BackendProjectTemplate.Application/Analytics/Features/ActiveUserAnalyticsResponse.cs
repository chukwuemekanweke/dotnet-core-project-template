namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record ActiveUserAnalyticsResponse(
    DateOnly From,
    DateOnly To,
    string ReportingTimezone,
    long DailyActiveUsers,
    long WeeklyActiveUsers,
    long MonthlyActiveUsers,
    decimal DailyToMonthlyPercentage,
    IReadOnlyList<ActiveUserAnalyticsTimelineItem> Timeline);
