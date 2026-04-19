namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record RefreshToken(string Value, DateTimeOffset ExpiresAtUtc);
