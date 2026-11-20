namespace BackendProjectTemplate.WebAPI.Features.Analytics;

public sealed record ActiveUserAnalyticsRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? TenantId = null);
