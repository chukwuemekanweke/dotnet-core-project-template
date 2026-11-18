namespace BackendProjectTemplate.WebAPI.Features.Authentication.AdminUsers;

public sealed record ListAdminUsersRequest(
    string? Search = null,
    string? Role = null,
    bool? IsLocked = null,
    Guid? TenantId = null,
    int Page = 1,
    int PageSize = 25);
