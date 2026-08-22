namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record SignUpOtpCommand(
    string Email,
    string Otp,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
