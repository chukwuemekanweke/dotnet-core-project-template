namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record SignUpOtpCommand(string Email, string Otp, ActorContext ActorContext);
