namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record RegistrationAnalyticsResponse(
    DateOnly From,
    DateOnly To,
    string ReportingTimezone,
    long AllTimeCount,
    long RegistrationCount,
    long PreviousRegistrationCount,
    decimal? PercentageChange,
    IReadOnlyList<RegistrationAnalyticsTimelineItem> Timeline);
