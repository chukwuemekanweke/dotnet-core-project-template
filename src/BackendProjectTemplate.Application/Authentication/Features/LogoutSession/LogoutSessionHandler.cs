using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;

namespace BackendProjectTemplate.Application.Authentication.Features.LogoutSession;

public sealed class LogoutSessionHandler(
    IAccessTokenRevocationService accessTokenRevocationService,
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
                Observability.EventNames.Authentication.UserSignedOut,
                new Dictionary<string, string>
                {
                    [Observability.StakeholderIdPropertyName] = request.StakeholderId.Value.ToString()
                });
        }

        return new LogoutSessionResult(LogoutSessionStatus.Success);
    }
}
