using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record TwoFactorChallenge(
    string Token,
    Guid AppUserId,
    Guid StakeholderId,
    Guid TenantId,
    AuthenticationMethod AuthenticationMethod,
    ActorContext ActorContext,
    string IpAddress,
    string UserAgent,
    DateTimeOffset ExpiresAtUtc,
    int RemainingAttempts);
