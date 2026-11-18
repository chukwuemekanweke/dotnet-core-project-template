using BackendProjectTemplate.Domain.Authentication.Administration;

namespace BackendProjectTemplate.Application.Authentication.Features.ListAdminUsers;

public sealed record ListAdminUsersQuery(
    string? Search,
    string? Role,
    bool? IsLocked,
    Guid? TenantId,
    int Page,
    int PageSize);

public sealed class ListAdminUsersHandler(IAdminUserReadModelRepository repository)
{
    public Task<AdminUserListPage> HandleAsync(ListAdminUsersQuery query, CancellationToken cancellationToken) =>
        repository.ListAsync(
            new AdminUserListRequest(
                query.Search,
                query.Role,
                query.IsLocked,
                query.TenantId,
                query.Page,
                query.PageSize),
            cancellationToken);
}
