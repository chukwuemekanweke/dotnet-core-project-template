namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserListItem(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    bool IsAdministrativelyLocked,
    DateTimeOffset? TemporaryLockoutEndUtc,
    DateTimeOffset CreatedAtUtc,
    AdminStakeholderSummary? Stakeholder,
    IReadOnlyList<string> Roles,
    AdminKycSummary? Kyc);
