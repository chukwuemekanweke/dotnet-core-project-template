namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

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
    AccountLocked = 9,
    RequiresTwoFactor = 10
}
