namespace BackendProjectTemplate.Application.Analytics;

public sealed record UserActivityObservation(
    Guid TenantId,
    Guid StakeholderId,
    DateTimeOffset OccurredAtUtc,
    string ActivityType);
