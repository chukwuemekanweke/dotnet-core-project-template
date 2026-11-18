using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.UnlockAdminUser;

public sealed record UnlockAdminUserCommand(Guid ActorUserId, Guid UserId, string Reason, ActorContext ActorContext);

public sealed class UnlockAdminUserHandler(IAdminUserAccountAdministration administration)
{
    public Task<AdminUserMutationResult> HandleAsync(UnlockAdminUserCommand command, CancellationToken cancellationToken) =>
        administration.UnlockAsync(
            new AdminUserAccountRequest(command.ActorUserId, command.UserId, command.Reason, command.ActorContext),
            cancellationToken);
}
