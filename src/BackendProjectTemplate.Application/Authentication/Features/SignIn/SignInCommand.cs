namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed record SignInCommand(
    string Email,
    string Password,
    string IpAddress,
    string UserAgent);
