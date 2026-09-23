using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.Authentication.ReadModels;

public sealed record LoginActivityHistoryReadModel(
    Guid LoginActivityId,
    LoginActivityType ActivityType,
    DateTimeOffset OccurredAtUtc,
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName,
    string? City,
    string? State,
    string? Country);
