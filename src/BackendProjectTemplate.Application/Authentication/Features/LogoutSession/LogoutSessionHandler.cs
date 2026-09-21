using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.LogoutSession;

public sealed class LogoutSessionHandler(
    IAccessTokenRevocationService accessTokenRevocationService,
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationSessionService sessionService,
    IUnitOfWork unitOfWork)
{
    public async Task<LogoutSessionResult> HandleAsync(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenId) || request.SessionId == Guid.Empty ||
            request.AppUserId == Guid.Empty)
        {
            return new LogoutSessionResult(LogoutSessionStatus.InvalidToken);
        }

        if (!await sessionService.RevokeAsync(request.SessionId, request.AppUserId, cancellationToken))
        {
            return new LogoutSessionResult(LogoutSessionStatus.InvalidToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await accessTokenRevocationService.RevokeAsync(request.TokenId, request.ExpiresAtUtc, cancellationToken);

        if (request.StakeholderId.HasValue)
        {
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.SignOutCompleted,
                ObservabilityEventProperties.Create(request.ActorContext, request.StakeholderId.Value));
        }

        return new LogoutSessionResult(LogoutSessionStatus.Success);
    }
}
