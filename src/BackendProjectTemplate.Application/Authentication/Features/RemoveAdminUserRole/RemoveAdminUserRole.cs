using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.RemoveAdminUserRole;

public sealed record RemoveAdminUserRoleCommand(
    Guid ActorUserId,
    Guid UserId,
    string RoleName,
    string Reason,
    ActorContext ActorContext);

public sealed class RemoveAdminUserRoleHandler(IAdminUserRoleAdministration administration)
{
    public Task<AdminUserMutationResult> HandleAsync(
        RemoveAdminUserRoleCommand command,
        CancellationToken cancellationToken) =>
        administration.RemoveAsync(
            new AdminUserRoleRequest(
                command.ActorUserId,
                command.UserId,
                command.RoleName,
                command.Reason,
                command.ActorContext),
            cancellationToken);
}
