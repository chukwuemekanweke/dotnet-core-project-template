namespace BackendProjectTemplate.Domain.Authentication.Administration;

public interface IAdminUserSessionAdministration
{
    Task<AdminUserMutationResult> RevokeAsync(AdminUserSessionRequest request, CancellationToken cancellationToken);
    Task<AdminUserMutationResult> RevokeAllAsync(AdminUserAccountRequest request, CancellationToken cancellationToken);
}
