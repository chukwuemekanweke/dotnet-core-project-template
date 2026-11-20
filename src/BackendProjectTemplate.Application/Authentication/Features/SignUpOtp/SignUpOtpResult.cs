using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public sealed record SignUpOtpResult(SignUpOtpStatus Status, AuthenticationTokens? Tokens = null);
