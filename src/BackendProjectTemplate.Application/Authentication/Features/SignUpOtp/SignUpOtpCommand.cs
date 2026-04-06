namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public sealed record SignUpOtpCommand(string Email, string Otp);
