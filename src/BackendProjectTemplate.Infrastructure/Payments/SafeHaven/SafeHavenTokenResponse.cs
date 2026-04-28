namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenTokenResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType,
    string? RefreshToken,
    string? Scope,
    string? IbsClientId);
