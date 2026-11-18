namespace BackendProjectTemplate.Domain.Authentication.Administration;

public interface IAdminUserAccountAdministration
{
    Task<AdminUserMutationResult> LockAsync(AdminUserAccountRequest request, CancellationToken cancellationToken);
    Task<AdminUserMutationResult> UnlockAsync(AdminUserAccountRequest request, CancellationToken cancellationToken);
}
