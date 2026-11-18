using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.RevokeAdminUserSession;

public sealed record RevokeAdminUserSessionCommand(
    Guid ActorUserId,
    Guid UserId,
    Guid SessionId,
    string Reason,
    ActorContext ActorContext);

public sealed class RevokeAdminUserSessionHandler(IAdminUserSessionAdministration administration)
{
    public Task<AdminUserMutationResult> HandleAsync(
        RevokeAdminUserSessionCommand command,
        CancellationToken cancellationToken) =>
        administration.RevokeAsync(
            new AdminUserSessionRequest(
                command.ActorUserId,
                command.UserId,
                command.SessionId,
                command.Reason,
                command.ActorContext),
            cancellationToken);
}
