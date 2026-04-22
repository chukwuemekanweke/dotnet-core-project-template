using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Application.Authentication.Features.LogoutSession;

public sealed class LogoutSessionHandler(
    IAccessTokenRevocationService accessTokenRevocationService,
    ICurrentActor currentActor,
    ICustomTelemetryContext customTelemetryContext)
{
    public async Task<LogoutSessionResult> HandleAsync(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenId))
        {
            return new LogoutSessionResult(LogoutSessionStatus.InvalidToken);
        }

        await accessTokenRevocationService.RevokeAsync(request.TokenId, request.ExpiresAtUtc, cancellationToken);

        if (request.StakeholderId.HasValue)
        {
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.SignOutCompleted,
                ObservabilityEventProperties.Create(currentActor, request.StakeholderId.Value));
        }

        return new LogoutSessionResult(LogoutSessionStatus.Success);
    }
}
