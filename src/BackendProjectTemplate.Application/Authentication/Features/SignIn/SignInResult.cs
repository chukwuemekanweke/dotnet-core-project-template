using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed record SignInResult(SignInStatus Status, AccessToken? AccessToken);

public enum SignInStatus
{
    Success = 1,
    InvalidCredentials = 2,
    EmailNotVerified = 3
}
