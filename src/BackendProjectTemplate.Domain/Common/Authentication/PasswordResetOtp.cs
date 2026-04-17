namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record PasswordResetOtp(string Code, DateTimeOffset ExpiresAtUtc);
