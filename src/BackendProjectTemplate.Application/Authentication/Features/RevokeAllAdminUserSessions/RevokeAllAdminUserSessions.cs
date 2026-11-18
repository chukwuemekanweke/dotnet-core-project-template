using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.RevokeAllAdminUserSessions;

public sealed record RevokeAllAdminUserSessionsCommand(
    Guid ActorUserId,
    Guid UserId,
    string Reason,
    ActorContext ActorContext);

public sealed class RevokeAllAdminUserSessionsHandler(IAdminUserSessionAdministration administration)
{
    public Task<AdminUserMutationResult> HandleAsync(
        RevokeAllAdminUserSessionsCommand command,
        CancellationToken cancellationToken) =>
        administration.RevokeAllAsync(
            new AdminUserAccountRequest(command.ActorUserId, command.UserId, command.Reason, command.ActorContext),
            cancellationToken);
}
