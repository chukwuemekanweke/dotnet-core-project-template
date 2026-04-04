namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed record SignInResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType);
