namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record ActiveUserListItem(
    Guid StakeholderId,
    Guid AppUserId,
    string? AvatarUrl,
    string FullName,
    string Email,
    DateTimeOffset FirstActivityAtUtc,
    DateTimeOffset LastActivityAtUtc,
    long ActivityCount,
    DateTimeOffset? LastLoginAtUtc);
