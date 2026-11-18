namespace BackendProjectTemplate.Domain.Authentication.Administration;

public interface IAdminUserRoleAdministration
{
    Task<AdminUserMutationResult> AssignAsync(AdminUserRoleRequest request, CancellationToken cancellationToken);
    Task<AdminUserMutationResult> RemoveAsync(AdminUserRoleRequest request, CancellationToken cancellationToken);
}
