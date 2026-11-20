using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.CompleteTwoFactorChallenge;

public sealed record CompleteTwoFactorChallengeResult(
    CompleteTwoFactorChallengeStatus Status,
    AuthenticationTokens? Tokens = null,
    DateTimeOffset? LockedUntilUtc = null);
