namespace BackendProjectTemplate.WebAPI.Features.Analytics;

public sealed record RegistrationAnalyticsRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    string Interval = "Day",
    Guid? TenantId = null);
