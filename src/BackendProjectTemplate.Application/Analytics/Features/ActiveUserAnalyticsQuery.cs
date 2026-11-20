namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record ActiveUserAnalyticsQuery(DateOnly? From, DateOnly? To, Guid? TenantId);
