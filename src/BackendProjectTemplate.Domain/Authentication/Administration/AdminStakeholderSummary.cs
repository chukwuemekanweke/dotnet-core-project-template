namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminStakeholderSummary(
    Guid StakeholderId,
    Guid TenantId,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsVerified);
