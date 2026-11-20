namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record ActiveUserListQuery(DateOnly Date, Guid? TenantId, int Page, int PageSize);
