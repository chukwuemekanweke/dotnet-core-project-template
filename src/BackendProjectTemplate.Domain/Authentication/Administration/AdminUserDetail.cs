namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserDetail(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    bool IsAdministrativelyLocked,
    DateTimeOffset? AdministrativelyLockedAtUtc,
    DateTimeOffset? TemporaryLockoutEndUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<string> Roles,
    AdminStakeholderSummary? Stakeholder,
    int ActiveSessionCount,
    AdminLoginSummary? LatestLogin,
    AdminKycSummary? Kyc,
    AdminSubscriptionSummary? Subscription);
