namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasskeySignIn;

public enum CompletePasskeySignInStatus
{
    Success = 1,
    RequiresTwoFactor = 2,
    InvalidFlow = 3,
    ExpiredFlow = 4,
    ConsumedFlow = 5,
    InvalidCredential = 6,
    AccountLocked = 7,
    EmailNotVerified = 8,
    TenantMismatch = 9
}
