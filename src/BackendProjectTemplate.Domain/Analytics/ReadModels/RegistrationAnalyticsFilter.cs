namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record RegistrationAnalyticsFilter(
    DateTimeOffset FromUtc,
    DateTimeOffset ToExclusiveUtc,
    Guid? TenantId);
