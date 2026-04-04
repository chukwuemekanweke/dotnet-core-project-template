namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public sealed record SignUpResponse(string Email, DateTimeOffset OtpExpiresAtUtc, string Message);
