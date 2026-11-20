namespace BackendProjectTemplate.WebAPI.Features.Analytics;

public sealed record ActiveUserListRequest(
    DateOnly? Date = null,
    Guid? TenantId = null,
    int Page = 1,
    int PageSize = 20);
