using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.RevokeOtherSessions;

public sealed class RevokeOtherSessionsHandler(IAuthenticationSessionService sessionService, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(RevokeOtherSessionsCommand command, CancellationToken cancellationToken)
    {
        await sessionService.RevokeOthersAsync(command.CurrentSessionId, command.AppUserId,
            command.StakeholderId, command.TenantId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
