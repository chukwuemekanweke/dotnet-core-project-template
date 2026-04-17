namespace BackendProjectTemplate.Domain.Stakeholders.ReadModels;

public sealed record StakeholderReadModel(
    Guid StakeholderId,
    Guid AppUserId,
    string EmailAddress,
    Guid TenantId,
    Guid CountryId,
    Guid StakeholderTypeId,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsVerified);
