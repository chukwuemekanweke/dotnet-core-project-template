namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

public enum GoogleSignUpStatus
{
    Success = 1,
    EmailVerificationRequired = 2,
    DuplicateEmail = 3,
    DuplicateGoogleAccount = 4,
    ValidationFailed = 5,
    CountryMismatch = 6,
    GoogleFlowInvalid = 7,
    GoogleFlowExpired = 8,
    GoogleFlowConsumed = 9
}
