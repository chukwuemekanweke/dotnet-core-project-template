using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface ITwoFactorChallengeService
{
    Task<TwoFactorChallenge> StartAsync(
        Guid appUserId,
        Guid stakeholderId,
        Guid tenantId,
        AuthenticationMethod authenticationMethod,
        ActorContext actorContext,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken);

    Task<TwoFactorChallengeResult> TakeAsync(string token, CancellationToken cancellationToken);
    Task RestoreAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken);
}
