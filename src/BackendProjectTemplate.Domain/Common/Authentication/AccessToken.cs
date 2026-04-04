namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
