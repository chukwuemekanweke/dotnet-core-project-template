namespace BackendProjectTemplate.Domain.Authentication.Administration;

public interface IAdminUserReadModelRepository
{
    Task<AdminUserListPage> ListAsync(AdminUserListRequest request, CancellationToken cancellationToken);
    Task<AdminUserDetail?> GetDetailAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>?> GetRolesAsync(Guid userId, CancellationToken cancellationToken);
}
