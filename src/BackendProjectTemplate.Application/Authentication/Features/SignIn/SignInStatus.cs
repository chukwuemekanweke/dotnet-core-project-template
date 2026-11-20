namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public enum SignInStatus
{
    Success = 1,
    InvalidCredentials = 2,
    EmailNotVerified = 3,
    AccountLocked = 4,
    RequiresTwoFactor = 5
}
