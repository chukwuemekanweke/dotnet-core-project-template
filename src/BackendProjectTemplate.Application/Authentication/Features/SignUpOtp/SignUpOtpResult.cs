using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public sealed record SignUpOtpResult(SignUpOtpStatus Status, AuthenticationTokens? Tokens = null);

public enum SignUpOtpStatus
{
    Success = 1,
    InvalidOtp = 2,
    AlreadyVerified = 3,
    AccountUnavailable = 4
}
