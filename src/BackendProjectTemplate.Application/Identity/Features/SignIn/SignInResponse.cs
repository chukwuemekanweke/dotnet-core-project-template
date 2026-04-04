namespace BackendProjectTemplate.Application.Identity.Features.SignIn;

public sealed record SignInResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType);
