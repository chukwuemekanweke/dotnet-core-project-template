namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record RegisteredUserListRequest(
    DateTimeOffset FromUtc,
    DateTimeOffset ToExclusiveUtc,
    Guid? TenantId,
    int Page,
    int PageSize);
