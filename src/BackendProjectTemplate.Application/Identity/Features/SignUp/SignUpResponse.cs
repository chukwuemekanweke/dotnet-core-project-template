namespace BackendProjectTemplate.Application.Identity.Features.SignUp;

public sealed record SignUpResponse(string Email, DateTimeOffset OtpExpiresAtUtc, string Message);
