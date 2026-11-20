namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record RegistrationAnalyticsTimelineItem(DateOnly From, DateOnly To, long Count);
