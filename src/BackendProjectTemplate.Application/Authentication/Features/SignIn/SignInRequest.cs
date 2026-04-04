namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed class SignInRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
