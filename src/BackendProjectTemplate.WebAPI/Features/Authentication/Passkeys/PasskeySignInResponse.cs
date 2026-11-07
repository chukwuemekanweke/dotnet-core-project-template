namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed record PasskeySignInResponse(
    string Outcome,
    string? AccessToken = null,
    DateTimeOffset? AccessTokenExpiresAtUtc = null,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAtUtc = null,
    string? TokenType = null,
    string? Challenge = null,
    DateTimeOffset? ChallengeExpiresAtUtc = null);
