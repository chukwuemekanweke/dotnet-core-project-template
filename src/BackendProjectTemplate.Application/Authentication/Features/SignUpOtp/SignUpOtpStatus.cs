namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public enum SignUpOtpStatus
{
    Success = 1,
    InvalidOtp = 2,
    AlreadyVerified = 3,
    AccountUnavailable = 4
}
