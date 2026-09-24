using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

public sealed record GoogleSignInResult(
    GoogleSignInStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null);

public enum GoogleSignInStatus
{
    Success = 1,
    LinkRequired = 2,
    RegistrationRequired = 3,
    InvalidGoogleCredential = 4,
    GoogleFlowInvalid = 5,
    GoogleFlowExpired = 6,
    GoogleFlowConsumed = 7,
    EmailVerificationRequired = 8,
    AccountLocked = 9
}
