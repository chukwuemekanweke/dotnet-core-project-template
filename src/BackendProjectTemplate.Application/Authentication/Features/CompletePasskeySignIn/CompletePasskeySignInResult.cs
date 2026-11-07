using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;

public sealed record CompletePasskeySignInResult(
    CompletePasskeySignInStatus Status,
    AuthenticationTokens? Tokens = null,
    TwoFactorChallenge? Challenge = null,
    DateTimeOffset? LockedUntilUtc = null);
