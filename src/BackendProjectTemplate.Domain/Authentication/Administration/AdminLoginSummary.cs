namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminLoginSummary(
    DateTimeOffset OccurredAtUtc,
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName);
