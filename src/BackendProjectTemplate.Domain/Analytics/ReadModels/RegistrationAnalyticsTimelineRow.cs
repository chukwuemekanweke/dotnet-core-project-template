namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record RegistrationAnalyticsTimelineRow(DateOnly PeriodStart, long Count);
