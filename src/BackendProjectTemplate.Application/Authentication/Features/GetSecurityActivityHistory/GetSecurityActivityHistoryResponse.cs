namespace BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;

public sealed record GetSecurityActivityHistoryResponse(
    Guid Id,
    string ActivityType,
    DateTimeOffset OccurredAtUtc,
    string IpAddress,
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName,
    string? City,
    string? State,
    string? Country,
    string? FailureReason);
