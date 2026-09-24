using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.LinkGoogleAccount;

public sealed record LinkGoogleAccountResult(
    LinkGoogleAccountStatus Status,
    AuthenticationTokens? Tokens = null,
    DateTimeOffset? LockedUntilUtc = null);

public enum LinkGoogleAccountStatus
{
    Success = 1,
    InvalidCredentials = 2,
    AccountLocked = 3,
    EmailVerificationRequired = 4,
    GoogleFlowInvalid = 5,
    GoogleFlowExpired = 6,
    GoogleFlowConsumed = 7,
    GoogleAccountAlreadyLinked = 8
}
