using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.AssignAdminUserRole;

public sealed record AssignAdminUserRoleCommand(
    Guid ActorUserId,
    Guid UserId,
    string RoleName,
    string Reason,
    ActorContext ActorContext);

public sealed class AssignAdminUserRoleHandler(IAdminUserRoleAdministration administration)
{
    public Task<AdminUserMutationResult> HandleAsync(
        AssignAdminUserRoleCommand command,
        CancellationToken cancellationToken) =>
        administration.AssignAsync(
            new AdminUserRoleRequest(
                command.ActorUserId,
                command.UserId,
                command.RoleName,
                command.Reason,
                command.ActorContext),
            cancellationToken);
}
