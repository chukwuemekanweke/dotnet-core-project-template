namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserListRequest(
    string? Search,
    string? Role,
    bool? IsLocked,
    Guid? TenantId,
    int Page,
    int PageSize);
