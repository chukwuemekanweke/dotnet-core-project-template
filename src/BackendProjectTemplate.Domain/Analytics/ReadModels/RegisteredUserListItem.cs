namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record RegisteredUserListItem(
    Guid StakeholderId,
    Guid AppUserId,
    string? AvatarUrl,
    string FullName,
    string Email,
    DateTimeOffset SignUpTimestamp,
    DateTimeOffset? LastLoginAtUtc);
