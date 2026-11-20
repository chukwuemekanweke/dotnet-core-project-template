using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.LinkGoogleAccount;

public sealed record LinkGoogleAccountResult(
    LinkGoogleAccountStatus Status,
    AuthenticationTokens? Tokens = null,
    DateTimeOffset? LockedUntilUtc = null);
