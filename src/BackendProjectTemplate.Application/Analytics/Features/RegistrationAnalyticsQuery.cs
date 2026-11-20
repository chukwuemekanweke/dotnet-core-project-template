using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record RegistrationAnalyticsQuery(
    DateOnly? From,
    DateOnly? To,
    Interval Interval,
    Guid? TenantId);
