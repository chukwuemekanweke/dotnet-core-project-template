using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.LockAdminUser;

public sealed record LockAdminUserCommand(Guid ActorUserId, Guid UserId, string Reason, ActorContext ActorContext);

public sealed class LockAdminUserHandler(IAdminUserAccountAdministration administration)
{
    public Task<AdminUserMutationResult> HandleAsync(LockAdminUserCommand command, CancellationToken cancellationToken) =>
        administration.LockAsync(
            new AdminUserAccountRequest(command.ActorUserId, command.UserId, command.Reason, command.ActorContext),
            cancellationToken);
}
