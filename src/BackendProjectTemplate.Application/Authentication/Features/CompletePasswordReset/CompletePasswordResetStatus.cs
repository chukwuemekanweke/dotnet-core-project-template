namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;

public enum CompletePasswordResetStatus
{
    Success = 1,
    InvalidOtp = 2,
    UserNotFound = 3,
    ValidationFailed = 4
}
