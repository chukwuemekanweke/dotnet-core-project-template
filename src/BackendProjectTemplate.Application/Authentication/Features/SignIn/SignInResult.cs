using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed record SignInResult(
    SignInStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null,
    TwoFactorChallenge? Challenge = null);
