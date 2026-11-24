namespace BackendProjectTemplate.Domain.Authentication.ReadModels;

public sealed record SecurityActivityHistoryCursorRequest(
    Guid TenantId,
    Guid StakeholderId,
    DateTimeOffset? CursorOccurredAtUtc,
    Guid? CursorSecurityActivityId,
    int Limit);
