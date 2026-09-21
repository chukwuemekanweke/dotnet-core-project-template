using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.RevokeSession;

public sealed class RevokeSessionHandler(IAuthenticationSessionService sessionService, IUnitOfWork unitOfWork)
{
    public async Task<bool> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionService.FindAsync(command.SessionId, cancellationToken);
        if (session is null || session.StakeholderId != command.StakeholderId)
        {
            return false;
        }

        if (!await sessionService.RevokeAsync(command.SessionId, command.StakeholderId, cancellationToken))
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
