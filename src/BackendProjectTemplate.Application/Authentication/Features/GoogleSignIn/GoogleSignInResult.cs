using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

public sealed record GoogleSignInResult(
    GoogleSignInStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null,
    TwoFactorChallenge? Challenge = null);
